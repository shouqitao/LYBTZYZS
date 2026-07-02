namespace LYBT.Module.Users.Domain.Services;

/// <summary>
/// 密码策略领域服务。封装密码复杂度验证规则。
/// </summary>
public static class PasswordPolicy
{
    /// <summary>最小密码长度</summary>
    public const int MinLength = 8;

    /// <summary>最大密码长度</summary>
    public const int MaxLength = 128;

    /// <summary>
    /// 验证密码是否符合策略要求。
    /// </summary>
    /// <param name="password">待验证密码</param>
    /// <returns>验证结果</returns>
    public static PasswordValidationResult Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("密码不能为空");
            return new PasswordValidationResult(false, errors);
        }

        if (password.Length < MinLength)
            errors.Add($"密码长度不能少于{MinLength}个字符");

        if (password.Length > MaxLength)
            errors.Add($"密码长度不能超过{MaxLength}个字符");

        if (!password.Any(char.IsUpper))
            errors.Add("密码必须包含至少一个大写字母");

        if (!password.Any(char.IsLower))
            errors.Add("密码必须包含至少一个小写字母");

        if (!password.Any(char.IsDigit))
            errors.Add("密码必须包含至少一个数字");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("密码必须包含至少一个特殊字符");

        return new PasswordValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 检查密码是否为常见弱密码。
    /// </summary>
    public static bool IsCommonPassword(string password)
    {
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password1!", "Admin123!", "Qwerty1!", "Letmein1!",
            "Welcome1!", "Monkey12!", "Dragon1!", "Master1!"
        };
        return commonPasswords.Contains(password);
    }
}

/// <summary>
/// 密码验证结果。
/// </summary>
public record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);


