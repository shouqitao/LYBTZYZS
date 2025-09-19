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
        /// 是否要求首次登录时更改密码
        /// </summary>
        public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;

        /// <summary>
        /// 是否启用账户锁定
        /// </summary>
        public bool EnableAccountLockout { get; set; } = false;
    }
}
