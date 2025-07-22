using System.ComponentModel;
namespace LYBT.Module.Auth.Dtos {
    /// <summary>
    /// 修改 sysadmin 密码 DTO
    /// </summary>
    public class ChangeSysAdminPasswordDto {
        /// <summary>
        /// 原密码
        /// </summary>
        [DisplayName("原密码")]
/// <summary>
/// OldPassword 属性。
/// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        [DisplayName("新密码")]
/// <summary>
/// NewPassword 属性。
/// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
