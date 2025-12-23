using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 默认密码配置
/// </summary>
public sealed class DefaultPasswordOptions
{
    public const string SectionName = ConfigurationSections.DefaultPasswords;

    /// <summary>
    /// 系统管理员默认密码
    /// </summary>
    [Required(ErrorMessage = "系统管理员默认密码不能为空")]
    [MinLength(8, ErrorMessage = "系统管理员默认密码长度不能少于8位")]
    public string SysAdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新用户默认密码
    /// </summary>
    [Required(ErrorMessage = "新用户默认密码不能为空")]
    [MinLength(8, ErrorMessage = "新用户默认密码长度不能少于8位")]
    public string NewUserPassword { get; set; } = string.Empty;

    /// <summary>
    /// 首次登录强制修改密码
    /// </summary>
    public bool ForceChangeOnFirstLogin { get; set; } = true;

    /// <summary>
    /// 开发环境是否启用默认密码功能
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;

    /// <summary>
    /// 生产环境是否允许使用默认密码（安全原因应始终为false）
    /// </summary>
    public bool AllowInProduction { get; set; } = false;

    /// <summary>
    /// 是否仅在数据库无用户时才使用默认密码
    /// </summary>
    public bool OnlyWhenDatabaseEmpty { get; set; } = true;

    /// <summary>
    /// 默认密码过期天数
    /// </summary>
    [Range(1, 365, ErrorMessage = "默认密码过期天数必须在1-365天之间")]
    public int ExpiryDays { get; set; } = 30;
}
