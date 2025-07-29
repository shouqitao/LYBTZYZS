using System.ComponentModel;

namespace LYBT.Common.Enums.Users {

    /// <summary>
    /// 用户账号状态
    /// </summary>
    [Description("用户状态")]
    public enum UserStatus {

        /// <summary>
        /// 正常
        /// </summary>
        [Description("正常")]
        Active = 0,

        /// <summary>
        /// 禁用
        /// </summary>
        [Description("已禁用")]
        Disabled = 1
    }
}