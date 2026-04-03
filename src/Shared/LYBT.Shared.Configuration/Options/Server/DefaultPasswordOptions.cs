using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 默认密码配置
/// </summary>
public sealed class DefaultPasswordOptions
{
    public const string SectionName = "DefaultPasswords";

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
}
