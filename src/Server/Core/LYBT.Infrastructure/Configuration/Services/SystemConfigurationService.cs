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
    /// 重新加载配置，触发 IOptionsMonitor&lt;T&gt; 热更新
    /// </summary>
    private void ReloadConfiguration()
    {
        if (_configuration is IConfigurationRoot root)
            root.Reload();
    }
}


