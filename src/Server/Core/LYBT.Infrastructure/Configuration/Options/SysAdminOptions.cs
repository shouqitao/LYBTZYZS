using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{

    /// <summary>
    /// 系统管理员配置选项
    /// </summary>
    public class SysAdminOptions
    {
        public const string SectionName = "SysAdminOptions";

        /// <summary>
        /// 默认系统管理员密码（已弃用，请使用DefaultPasswords:SystemAdmin）
        /// </summary>
        [Obsolete("请使用 DefaultPasswordOptions.SystemAdmin 替代", true)]
        [Required(ErrorMessage = "系统管理员默认密码不能为空")]
        [MinLength(8, ErrorMessage = "系统管理员默认密码长度至少8个字符")]
        public string DefaultPassword { get; set; } = "Admin@123456";

        /// <summary>
        /// 是否要求首次登录时更改密码
        /// </summary>
        public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;

        /// <summary>
        /// 是否启用账户锁定
        /// </summary>
        public bool EnableAccountLockout { get; set; } = false;
    }
}
