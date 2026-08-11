using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Configuration.Security;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Configuration.Validation;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Configuration.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationStore _store;
    private readonly ProductionConfigurationValidator _validator;
    private readonly ILogger<SystemConfigurationService> _logger;

    public SystemConfigurationService(
        IConfiguration configuration,
        IConfigurationStore store,
        ProductionConfigurationValidator validator,
        ILogger<SystemConfigurationService> logger)
    {
        _configuration = configuration;
        _store = store;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Dictionary<string, string>>> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = new Dictionary<string, string>();

        // Return safe, non-sensitive configuration items
        config["App:Name"] = _configuration["App:Name"] ?? "LYBTZYZS";
        config["App:Version"] = _configuration["App:Version"] ?? "1.0.0";
        config["App:Environment"] = _configuration["App:Environment"] ?? "Production";

        _logger.LogInformation("[SVC] SystemConfiguration.GetConfiguration completed");
        return await Task.FromResult(Result<Dictionary<string, string>>.Success(config));
    }

    public async Task<Result<string?>> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return await Task.FromResult(Result<string?>.Failure("配置项名称不能为空"));

        var value = _configuration[key];
        _logger.LogInformation("[SVC] SystemConfiguration.GetValue - Key={Key}", key);
        return await Task.FromResult(Result<string?>.Success(value));
    }

    public async Task<Result> ValidateProductionConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _validator.ValidateOrThrow();
            _logger.LogInformation("[SVC] SystemConfiguration.ValidateProduction - PASSED");
            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SVC] SystemConfiguration.ValidateProduction - FAILED");
            return await Task.FromResult(Result.Failure(ex.Message));
        }
    }

    public async Task<Result> SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return await Task.FromResult(Result.Failure("配置项名称不能为空"));

        if (!ConfigurationWritePolicy.IsAllowed(key))
            return await Task.FromResult(Result.Failure($"配置项 '{key}' 不在允许修改的白名单内，禁止修改"));

        try
        {
            await _store.SetValueAsync(key, value, cancellationToken);
            ReloadConfiguration();
            _logger.LogInformation("[SVC] SystemConfiguration.SetValue - Key={Key}", key);
            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] SystemConfiguration.SetValue - FAILED Key={Key}", key);
            return await Task.FromResult(Result.FromException(ex, "修改配置"));
        }
    }

    public async Task<Result> UpdateConfigurationAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        if (settings is null || settings.Count == 0)
            return await Task.FromResult(Result.Failure("配置项集合不能为空"));

        var invalidKeys = settings.Keys
            .Where(k => string.IsNullOrWhiteSpace(k) || !ConfigurationWritePolicy.IsAllowed(k))
            .ToList();
        if (invalidKeys.Count > 0)
            return await Task.FromResult(Result.Failure($"以下配置项不在允许修改的白名单内，禁止修改: {string.Join(", ", invalidKeys)}"));

        try
        {
            foreach (var kv in settings)
                await _store.SetValueAsync(kv.Key, kv.Value, cancellationToken);

            ReloadConfiguration();
            _logger.LogInformation("[SVC] SystemConfiguration.UpdateConfiguration - Count={Count}", settings.Count);
            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] SystemConfiguration.UpdateConfiguration - FAILED Count={Count}", settings.Count);
            return await Task.FromResult(Result.FromException(ex, "批量修改配置"));
        }
    }

    /// <summary>
    /// 获取单节配置（SHELL-018 Phase 1: 敏感键掩码）
    /// </summary>
    public async Task<Result<Dictionary<string, string>>> GetSectionAsync(string section, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(section))
            return await Task.FromResult(Result<Dictionary<string, string>>.Failure("配置节名称不能为空"));

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var configSection = _configuration.GetSection(section);
        foreach (var child in configSection.GetChildren())
            result[child.Key] = ConfigurationWritePolicy.IsSensitive($"{section}:{child.Key}")
                ? "***"
                : child.Value ?? string.Empty;

        _logger.LogInformation("[SVC] SystemConfiguration.GetSection - Section={Section} Keys={Count}", section, result.Count);
        return await Task.FromResult(Result<Dictionary<string, string>>.Success(result));
    }

    /// <summary>
    /// 批量修改单节配置（SHELL-018 Phase 1: 白名单逐键 + 持久化 + Reload）
    /// </summary>
    public async Task<Result<ConfigUpdateResultDto>> UpdateSectionAsync(string section, Dictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(section))
            return await Task.FromResult(Result<ConfigUpdateResultDto>.Failure("配置节名称不能为空"));
        if (values is null || values.Count == 0)
            return await Task.FromResult(Result<ConfigUpdateResultDto>.Failure("配置项集合不能为空"));

        var fullKeys = values.ToDictionary(
            kv => $"{section}:{kv.Key}",
            kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        var invalidKeys = fullKeys.Keys
            .Where(k => !ConfigurationWritePolicy.IsAllowed(k))
            .ToList();
        if (invalidKeys.Count > 0)
            return await Task.FromResult(Result<ConfigUpdateResultDto>.Failure($"以下配置项不在允许修改的白名单内，禁止修改: {string.Join(", ", invalidKeys)}"));

        try
        {
            foreach (var kv in fullKeys)
                await _store.SetValueAsync(kv.Key, kv.Value, cancellationToken);

            ReloadConfiguration();

            // 生效语义：功能开关节热更新（FeatureToggles），其余重启
            var isHotReload = section.Equals("FeatureToggles", StringComparison.OrdinalIgnoreCase)
                || section.Equals("ClinicSettings", StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("[SVC] SystemConfiguration.UpdateSection - Section={Section} Count={Count} HotReload={Hot}",
                section, fullKeys.Count, isHotReload);

            return await Task.FromResult(Result<ConfigUpdateResultDto>.Success(new ConfigUpdateResultDto
            {
                Applied = true,
                RestartRequired = !isHotReload,
                EffectiveMode = isHotReload ? "hot" : "restart",
                UpdatedCount = fullKeys.Count
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SVC] SystemConfiguration.UpdateSection - FAILED Section={Section}", section);
            return await Task.FromResult(Result<ConfigUpdateResultDto>.FromException(ex, "修改配置节"));
        }
    }

    /// <summary>
    /// 重新加载配置，触发 IOptionsMonitor&lt;T&gt; 热更新
    /// </summary>
    private void ReloadConfiguration()
    {
        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }
}


