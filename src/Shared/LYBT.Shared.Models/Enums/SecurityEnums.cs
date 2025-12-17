using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 密码强度等级枚举
    /// OpenSpec: unify-enums-to-shared - 从PasswordHelper.cs迁移
    /// </summary>
    public enum PasswordStrength
    {
        /// <summary>弱密码</summary>
        [Description("弱")]
        Weak = 1,

        /// <summary>一般密码</summary>
        [Description("一般")]
        Fair = 2,

        /// <summary>良好密码</summary>
        [Description("良好")]
        Good = 3,

        /// <summary>强密码</summary>
        [Description("强")]
        Strong = 4,

        /// <summary>很强密码</summary>
        [Description("很强")]
        VeryStrong = 5
    }
}
