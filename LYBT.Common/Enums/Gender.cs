using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 性别枚举（公共枚举类）
    /// </summary>
    public enum Gender {

        [Description("男")]
        Male = 0,

        [Description("女")]
        Female = 1,

        [Description("未知")]
        Unknown = 2
    }
}