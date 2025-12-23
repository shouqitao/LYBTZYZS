using LYBT.Shared.Configuration.Options.Common;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// JWT 配置自定义验证器
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        // 验证 SecretKey 是否为有效的 Base64
        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            try
            {
                var bytes = Convert.FromBase64String(options.SecretKey);
                if (bytes.Length < 32)
                {
                    failures.Add("JWT SecretKey 解码后长度必须至少为 32 字节");
                }
            }
            catch (FormatException)
            {
                failures.Add("JWT SecretKey 必须是有效的 Base64 字符串");
            }
        }

        // 验证 AccessToken 过期时间小于 RefreshToken
        if (options.AccessTokenExpirationMinutes >= options.RefreshTokenExpirationDays * 24 * 60)
        {
            failures.Add("AccessToken 过期时间必须小于 RefreshToken 过期时间");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
