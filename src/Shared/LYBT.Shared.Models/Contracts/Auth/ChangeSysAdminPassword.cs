using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 修改 sysadmin 密码 - 前后端共享API契约
    /// </summary>
    public class ChangeSysAdminPassword
    {

        /// <summary>
        /// 原密码
        /// </summary>
        [DisplayName("原密码")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
