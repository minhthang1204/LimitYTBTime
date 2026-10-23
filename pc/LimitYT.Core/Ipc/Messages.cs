using System.Text.Json;

namespace LimitYT.Core.Ipc;

public static class PipeNames
{
    /// <summary>Agent (tài khoản người dùng) ↔ Service.</summary>
    public const string Agent = "LimitYT.Agent";
    /// <summary>CLI quản trị ↔ Service. Chỉ Administrators mở được.</summary>
    public const string Admin = "LimitYT.Admin";
}

public sealed record AgentHeartbeat(bool Watching, string? MatchedService);

public sealed record ServicePatterns(string ServiceId, List<string> Patterns);

public sealed record AgentReply(
    bool Blocked,
    string Message,
    int LeftMinutes,
    int? WarnMinutes,
    int ConfigVersion,
    List<ServicePatterns> Patterns,
    List<string> TitleExcludes,
    List<string> Executables);

public sealed record AdminRequest(List<string> Args);

public sealed record AdminReply(bool Ok, string Message);

/// <summary>Mỗi thông điệp là một dòng JSON.</summary>
public static class PipeJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string line) => JsonSerializer.Deserialize<T>(line, Options);
}
