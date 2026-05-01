using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LYBT.Infrastructure.Configuration.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ProductionConfigurationValidator _validator;
    private readonly ILogger<SystemConfigurationService> _logger;

    public SystemConfigurationService(
        IConfiguration configuration,
        ProductionConfigurationValidator validator,
        ILogger<SystemConfigurationService> logger)
    {
        _configuration = configuration;
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
            return await Task.FromResult(Result<string?>.Fail("配置项名称不能为空"));

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
            return await Task.FromResult(Result.Fail(ex.Message));
        }
    }
}
