using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// LocalJwt 配置验证器
/// </summary>
public sealed class LocalJwtOptionsValidator : IValidateOptions<LocalJwtOptions>
{
    public ValidateOptionsResult Validate(string? name, LocalJwtOptions options)
    {
        var failures = new List<string>();

        if (!string.IsNullOrEmpty(options.SecretKey))
        {
            try
            {
                var keyBytes = Convert.FromBase64String(options.SecretKey);
                if (keyBytes.Length < 32)
                    failures.Add("LocalJwt:SecretKey 解码后长度不能小于 32 字节");
            }
            catch (FormatException)
            {
                failures.Add("LocalJwt:SecretKey 必须是有效的 Base64 字符串");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
