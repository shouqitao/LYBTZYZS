using System.Collections.Concurrent;
using System.Text.Json;

namespace LYBT.Infrastructure.Configuration.Stores;

/// <summary>
/// 基于 JSON 文件的运行时配置覆盖存储
/// 存储路径: {BaseDirectory}/config/runtime-overrides.json
/// 只持久化与 appsettings.json 默认值不同的项，保证覆盖文件最小化
/// </summary>
public sealed class JsonFileConfigurationStore : IConfigurationStore, IDisposable
{
    private readonly string _filePath;
    private readonly IReadOnlyDictionary<string, string?> _baseline;
    private readonly ConcurrentDictionary<string, string> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <param name="filePath">覆盖文件路径，默认 {BaseDirectory}/config/runtime-overrides.json</param>
    /// <param name="baseline">appsettings 默认值快照，用于判断覆盖项是否与默认值相同</param>
    public JsonFileConfigurationStore(string? filePath = null, IReadOnlyDictionary<string, string?>? baseline = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "config", "runtime-overrides.json");
        _baseline = baseline ?? new Dictionary<string, string?>();
        LoadFromFile();
    }

    public async Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            return new Dictionary<string, string>(_overrides, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("配置项名称不能为空", nameof(key));
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        // 与默认值相同 → 移除覆盖，恢复默认（不持久化冗余项）
        if (_baseline.TryGetValue(key, out var defaultValue) && defaultValue == value)
        {
            await RemoveAsync(key, cancellationToken);
            return;
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            _overrides[key] = value;
            await PersistAsync(cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            _overrides.TryRemove(key, out _);
            await PersistAsync(cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public void Dispose()
    {
        _fileLock.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        // 覆盖为空时不落盘，保持目录干净
        if (_overrides.IsEmpty)
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(
            _overrides.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
            new JsonSerializerOptions { WriteIndented = true });

        // 原子写入：先写临时文件再替换，避免崩溃时留下损坏文件
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    private void LoadFromFile()
    {
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (data is null)
            return;

        foreach (var kv in data)
            _overrides[kv.Key] = kv.Value;
    }
}
