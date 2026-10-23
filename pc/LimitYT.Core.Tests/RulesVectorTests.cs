using System.Text.Json;
using System.Text.Json.Nodes;
using LimitYT.Core.Config;
using LimitYT.Core.Rules;
using LimitYT.Core.Usage;

namespace LimitYT.Core.Tests;

/// <summary>Chạy spec/rules-test-vectors.json — cùng bộ vector mà bản Kotlin trên TV phải pass.</summary>
public class RulesVectorTests
{
    private static readonly JsonObject Root =
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "rules-test-vectors.json")))!.AsObject();

    public static TheoryData<string> CaseNames()
    {
        var data = new TheoryData<string>();
        foreach (var c in Root["cases"]!.AsArray()) data.Add(c!["name"]!.GetValue<string>());
        return data;
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Vector(string name)
    {
        var c = Root["cases"]!.AsArray().Single(x => x!["name"]!.GetValue<string>() == name)!.AsObject();

        var configNode = Root["baseConfig"]!.DeepClone().AsObject();
        if (c["config"] is JsonObject overrides)
            foreach (var (key, value) in overrides)
                configNode[key] = value?.DeepClone();
        var config = configNode.Deserialize<LimitConfig>(ConfigJson.Options)!;

        var ledger = new UsageLedger();
        foreach (var u in c["usage"]?.AsArray() ?? [])
            ledger.Add(u!["device"]!.GetValue<string>(), DateOnly.Parse(u["date"]!.GetValue<string>()), u["minutes"]!.GetValue<long>() * 60_000);
        foreach (var a in c["adjustments"]?.AsArray() ?? [])
            ledger.Merge(new Adjustment(a!["id"]!.GetValue<string>(), DateOnly.Parse(a["date"]!.GetValue<string>()),
                a["minutes"]!.GetValue<long>() * 60_000, null, DateTimeOffset.UnixEpoch));

        OfflineState? offline = c["offline"] is JsonObject o
            ? new OfflineState(o["budgetMinutes"]!.GetValue<long>() * 60_000, o["selfWeekUsedAtStartMinutes"]!.GetValue<long>() * 60_000)
            : null;
        DateTimeOffset? unlockedUntil = c["unlockedUntil"] is JsonValue u2 ? DateTimeOffset.Parse(u2.GetValue<string>()) : null;
        var self = c["self"]?.GetValue<string>() ?? Devices.Pc;

        var decision = new RulesEngine(config).Evaluate(DateTimeOffset.Parse(c["now"]!.GetValue<string>()), ledger, self, offline, unlockedUntil);

        var expect = c["expect"]!.AsObject();
        Assert.Equal(expect["allowed"]!.GetValue<bool>(), decision.Allowed);
        if (expect["leftMinutes"] is JsonValue left)
            Assert.Equal(left.GetValue<int>(), (int)Math.Floor(decision.Left.TotalMinutes));
        if (expect["reason"] is JsonValue reason)
            Assert.Equal(reason.GetValue<string>(), decision.Reason.ToString());
        if (expect.ContainsKey("nextChange"))
        {
            DateTimeOffset? expected = expect["nextChange"] is JsonValue n ? DateTimeOffset.Parse(n.GetValue<string>()) : null;
            Assert.Equal(expected, decision.NextChangeUtc);
        }
    }
}
