using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 密码策略配置
/// </summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    /// <summary>
    /// 最小长度
    /// </summary>
    [Range(6, 32)]
    public int MinLength { get; set; } = 8;

    /// <summary>
    /// 是否需要数字
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// 是否需要小写字母
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// 是否需要大写字母
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// 是否需要特殊字符
    /// </summary>
    public bool RequireSpecialChar { get; set; } = true;
}
