using System.ComponentModel;

namespace LYBT.Common.Enums {
    /// <summary>
    /// 医生职称枚举
    /// </summary>
    public enum DoctorTitle {
        [Description("初级")]
        Junior = 0,

        [Description("中级")]
        Intermediate = 1,

        [Description("高级")]
        Senior = 2
    }
}
