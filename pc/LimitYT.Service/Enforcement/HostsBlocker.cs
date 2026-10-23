using System.Text;
using LimitYT.Core.Blocking;
using LimitYT.Service.Native;

namespace LimitYT.Service.Enforcement;

public sealed class HostsBlocker(ILogger<HostsBlocker> log)
{
    public static readonly string HostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");

    /// <summary>Đảm bảo khối chặn có/không có trong hosts. Chỉ ghi khi nội dung khác.</summary>
    /// <returns>Thông báo lỗi, hoặc null nếu thành công.</returns>
    public string? Ensure(IReadOnlyCollection<string> domains, bool blocked)
    {
        try
        {
            var current = File.Exists(HostsPath) ? File.ReadAllText(HostsPath) : "";
            var desired = blocked ? HostsFile.Apply(current, domains) : HostsFile.Remove(current);
            if (desired == current) return null;
            if (AppPaths.DryRun)
            {
                log.LogInformation("[dry-run] {Action} khối chặn trong hosts", blocked ? "Sẽ thêm" : "Sẽ gỡ");
                return null;
            }

            var attributes = File.Exists(HostsPath) ? File.GetAttributes(HostsPath) : FileAttributes.Normal;
            if (attributes.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(HostsPath, attributes & ~FileAttributes.ReadOnly);

            File.WriteAllText(HostsPath, desired, new UTF8Encoding(false));
            NativeMethods.DnsFlushResolverCache();
            log.LogInformation(blocked ? "Đã thêm khối chặn vào hosts" : "Đã gỡ khối chặn khỏi hosts");
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            log.LogWarning(e, "Không ghi được file hosts");
            return $"Không ghi được file hosts ({e.Message}). Kiểm tra phần mềm diệt virus.";
        }
    }
}
