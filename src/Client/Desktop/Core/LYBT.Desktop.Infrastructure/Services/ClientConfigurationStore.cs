using System.IO;
using System.Text.Json;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 客户端配置存储（SHELL-018 Phase 2: appsettings.json / clinic-settings.json / feature-toggles.json 节级原子写）
/// IConfiguration 是只读快照——本存储直接改 JSON 文件 + 触发 Reload 实现「保存即生效」。
/// </summary>
public class ClientConfigurationStore : IClientConfigurationStore
{
    private const string AppSettingsFile = "appsettings.json";
    private const string ClinicSettingsFile = "clinic-settings.json";
    private const string FeatureTogglesFile = "feature-toggles.json";

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<ClientConfigurationStore> _logger;

    public ClientConfigurationStore(
        IConfiguration configuration,
        ILogger<ClientConfigurationStore> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SaveSectionAsync(string section, IReadOnlyDictionary<string, object> values)
    {
        if (string.IsNullOrWhiteSpace(section) || values is null || values.Count == 0)
            return false;

        try
        {
            var filePath = ResolveFilePath(section);
            var root = await ReadJsonAsync(filePath);

            // 节级覆盖：目标节替换为传入字典（保留其他节）
            root[section] = JsonSerializer.SerializeToElement(values, JsonWriteOptions);

            var json = JsonSerializer.Serialize(root, JsonWriteOptions);
            await AtomicWriteAsync(filePath, json);

            // reloadOnChange 已配置——显式 Reload 双保险（覆盖文件替换竞态）
            if (_configuration is IConfigurationRoot rootConfig)
                rootConfig.Reload();

            _logger.LogInformation("[CFG-STORE] Section '{Section}' saved - {FilePath} ({Count} keys)", section, filePath, values.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CFG-STORE] Save section '{Section}' failed", section);
            return false;
        }
    }

    /// <inheritdoc />
    public string ResolveFilePath(string section)
    {
        var baseDir = AppContext.BaseDirectory;
        return section switch
        {
            "ClinicSettings" => Path.Combine(baseDir, ClinicSettingsFile),
            "FeatureToggles" => Path.Combine(baseDir, FeatureTogglesFile),
            _ => Path.Combine(baseDir, AppSettingsFile)
        };
    }

    private static async Task<Dictionary<string, JsonElement>> ReadJsonAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, JsonElement>();

        var text = await File.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(text))
            return new Dictionary<string, JsonElement>();

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    private static async Task AtomicWriteAsync(string filePath, string json)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);

        // 备份原文件（可回滚）
        if (File.Exists(filePath))
            File.Copy(filePath, $"{filePath}.bak.{DateTime.UtcNow:yyyyMMddHHmmss}", overwrite: true);

        File.Move(tempPath, filePath, overwrite: true);
    }
}
