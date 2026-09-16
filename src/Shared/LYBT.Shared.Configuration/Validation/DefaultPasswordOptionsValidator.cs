using System;
using System.Collections.Generic;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Options;

namespace LYBT.Shared.Configuration.Validation;

/// <summary>
/// DefaultPasswordOptions 自定义验证器
/// H-7: 检查密码值是否包含未展开的 ${} 占位符（环境变量未注入时视为配置缺失）
/// </summary>
public sealed class DefaultPasswordOptionsValidator : IValidateOptions<DefaultPasswordOptions>
{
    public ValidateOptionsResult Validate(string? name, DefaultPasswordOptions options)
    {
        var failures = new List<string>();

        CheckPlaceholder(nameof(options.SysAdminPassword), options.SysAdminPassword, failures);
        CheckPlaceholder(nameof(options.AdminPassword), options.AdminPassword, failures);
        CheckPlaceholder(nameof(options.NewUserPassword), options.NewUserPassword, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void CheckPlaceholder(string propertyName, string? value, List<string> failures)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.StartsWith("${", StringComparison.Ordinal)
            && value.EndsWith("}", StringComparison.Ordinal))
        {
            failures.Add($"DefaultPasswords:{propertyName} 仍为未展开占位符 {value}（需注入环境变量 DefaultPasswords__{propertyName}）");
        }
    }
}
