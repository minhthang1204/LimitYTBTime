using System.Text.Json;
using LimitYT.Core.Config;

namespace LimitYT.Service.Storage;

/// <summary>Đọc/ghi config.json. Phụ huynh có thể sửa file bằng tay; Service tự nạp lại khi file đổi.</summary>
public sealed class ConfigStore(string path, TimeProvider time, ILogger<ConfigStore> log)
{
    private DateTime _loadedWriteTime;

    public LimitConfig Load()
    {
        if (!File.Exists(path))
        {
            var created = Defaults.Create();
            created.UpdatedAtUtc = time.GetUtcNow();
            Write(created);
            log.LogInformation("Đã tạo cấu hình mặc định tại {Path}", path);
            return created;
        }

        try
        {
            return Read();
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or IOException)
        {
            log.LogError(e, "Cấu hình {Path} không hợp lệ, tạm dùng cấu hình mặc định", path);
            _loadedWriteTime = File.GetLastWriteTimeUtc(path);
            return Defaults.Create();
        }
    }

    /// <returns>true nếu file đã đổi và nạp lại thành công.</returns>
    public bool TryReload(out LimitConfig config)
    {
        config = null!;
        if (!File.Exists(path) || File.GetLastWriteTimeUtc(path) == _loadedWriteTime) return false;
        try
        {
            config = Read();
            log.LogInformation("Đã nạp lại cấu hình (version {Version})", config.Version);
            return true;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or IOException)
        {
            log.LogError(e, "Cấu hình mới không hợp lệ, giữ cấu hình cũ");
            _loadedWriteTime = File.GetLastWriteTimeUtc(path);
            return false;
        }
    }

    /// <summary>Tăng version và ghi xuống file.</summary>
    public void Save(LimitConfig config)
    {
        config.Version++;
        config.UpdatedAtUtc = time.GetUtcNow();
        Write(config);
    }

    private LimitConfig Read()
    {
        var config = JsonSerializer.Deserialize<LimitConfig>(File.ReadAllText(path), ConfigJson.Options)
                     ?? throw new InvalidDataException("File cấu hình rỗng");
        var errors = ConfigValidator.Validate(config);
        if (errors.Count > 0) throw new InvalidDataException(string.Join(Environment.NewLine, errors));
        _loadedWriteTime = File.GetLastWriteTimeUtc(path);
        return config;
    }

    private void Write(LimitConfig config)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, ConfigJson.Options));
        File.Move(tmp, path, overwrite: true);
        _loadedWriteTime = File.GetLastWriteTimeUtc(path);
    }
}
