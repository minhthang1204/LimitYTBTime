using LimitYT.Core.Config;
using LimitYT.Service;
using LimitYT.Service.Enforcement;
using LimitYT.Service.Ipc;
using LimitYT.Service.Storage;
using Microsoft.Extensions.Logging.Abstractions;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--cleanup"))
{
    // Dùng khi gỡ cài đặt: xoá khối chặn trong hosts và mọi policy trình duyệt LimitYT đã ghi.
    var config = File.Exists(AppPaths.ConfigFile)
        ? new ConfigStore(AppPaths.ConfigFile, TimeProvider.System, NullLogger<ConfigStore>.Instance).Load()
        : Defaults.Create();
    var domains = config.AllDomains.ToList();
    var error = new HostsBlocker(NullLogger<HostsBlocker>.Instance).Ensure(domains, blocked: false);
    new BrowserPolicy(NullLogger<BrowserPolicy>.Instance).RemoveAll(domains);
    Console.WriteLine(error ?? "Đã gỡ khối chặn trong hosts và policy trình duyệt.");
    return error is null ? 0 : 1;
}

AppPaths.DryRun = args.Contains("--dry-run");
Directory.CreateDirectory(AppPaths.DataDir);

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "LimitYT");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new ConfigStore(AppPaths.ConfigFile, sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<ConfigStore>>()));
builder.Services.AddSingleton(_ => new SqliteStore(AppPaths.DbFile));
builder.Services.AddSingleton<LimitController>();
builder.Services.AddSingleton<HostsBlocker>();
builder.Services.AddSingleton<BrowserPolicy>();
builder.Services.AddSingleton<AgentLauncher>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<AgentPipeServer>();
builder.Services.AddHostedService<AdminPipeServer>();

builder.Build().Run();
return 0;
