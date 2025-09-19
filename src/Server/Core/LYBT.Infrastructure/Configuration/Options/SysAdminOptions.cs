using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 系统管理员配置选项
    /// 支持可配置的sysadmin用户名，用于隐藏和保护超级管理员身份
    /// </summary>
    public class SysAdminOptions
    {
        public const string SectionName = "SysAdminOptions";

        /// <summary>
        /// 系统管理员用户名（可配置，用于隐藏默认的"sysadmin"）
        /// </summary>
        [Required(ErrorMessage = "系统管理员用户名不能为空")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "系统管理员用户名长度必须在3-50字符之间")]
        public string Username { get; set; } = "sysadmin";

        /// <summary>
        /// 系统管理员默认密码
        /// </summary>
        [Required(ErrorMessage = "系统管理员默认密码不能为空")]
        [MinLength(8, ErrorMessage = "系统管理员默认密码长度至少8个字符")]
        public string DefaultPassword { get; set; } = "LybtAdmin2025@SecurePass!";

        /// <summary>
        /// 是否要求首次登录时更改密码
        /// </summary>
        public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;

        /// <summary>
        /// 是否启用账户锁定
        /// </summary>
        public bool EnableAccountLockout { get; set; } = false;

        /// <summary>
        /// 基础配置验证
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Username))
                throw new InvalidOperationException("系统管理员用户名不能为空");

            if (Username.Length < 3 || Username.Length > 50)
                throw new InvalidOperationException("系统管理员用户名长度必须在3-50字符之间");

            if (string.IsNullOrWhiteSpace(DefaultPassword))
                throw new InvalidOperationException("系统管理员默认密码不能为空");

            if (DefaultPassword.Length < 8)
                throw new InvalidOperationException("系统管理员默认密码长度至少8个字符");
        }
    }
}
