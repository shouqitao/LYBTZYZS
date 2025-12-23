using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// 安全配置验证器
/// </summary>
public sealed class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        var failures = new List<string>();

        // 验证速率限制配置
        if (options.RateLimiting.Enabled)
        {
            if (options.RateLimiting.LoginLimit.InternalPermitLimit < options.RateLimiting.LoginLimit.PermitLimit)
            {
                failures.Add("LoginLimit.InternalPermitLimit 不应小于 PermitLimit");
            }

            if (options.RateLimiting.ApiLimit.AdminPermitLimit < options.RateLimiting.ApiLimit.PermitLimit)
            {
                failures.Add("ApiLimit.AdminPermitLimit 不应小于 PermitLimit");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
