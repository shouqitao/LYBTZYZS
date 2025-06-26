using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 挂号类型枚举（英文命名，描述为中文）
    /// </summary>
    public enum RegistrationType {

        [Description("普通挂号")]
        General = 0,

        [Description("急诊挂号")]
        Emergency = 1,

        [Description("复诊挂号")]
        FollowUp = 2
    }
}