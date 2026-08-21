using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// LocalJwt 配置验证器（A-28 P1-7: 配置节统一为 Jwt）
/// </summary>
public sealed class LocalJwtOptionsValidator : IValidateOptions<LocalJwtOptions>
{
    private readonly IConfiguration? _configuration;

    public LocalJwtOptionsValidator() { }
    public LocalJwtOptionsValidator(IConfiguration configuration) => _configuration = configuration;

    public ValidateOptionsResult Validate(string? name, LocalJwtOptions options)
    {
        var failures = new List<string>();

        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            try
            {
                var keyBytes = Convert.FromBase64String(options.SecretKey);
                if (keyBytes.Length < 32)
                    failures.Add("Jwt:SecretKey 解码后长度不能小于 32 字节");
            }
            catch (FormatException)
            {
                failures.Add("Jwt:SecretKey 必须是有效的 Base64 字符串");
            }
        }

        // P0-4 ADR-0024: 生产环境双密钥隔离 — LocalJwt Secret 必须与远程 Jwt 不同值
        var env = _configuration?["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isProd = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
        if (isProd && _configuration != null)
        {
            var remoteKey = _configuration["Jwt:SecretKey"];
            if (!string.IsNullOrWhiteSpace(remoteKey)
                && !string.IsNullOrWhiteSpace(options.SecretKey)
                && string.Equals(remoteKey.Trim(), options.SecretKey.Trim(), StringComparison.Ordinal))
            {
                failures.Add("Jwt:SecretKey（Local）不能与远程 Jwt:SecretKey 相同（ADR-0024 双密钥隔离生产强校验）");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
