using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 挂号状态枚举
    /// </summary>
    [Description("挂号状态")]
    public enum RegistrationStatus {

        [Description("待看诊")]
        Pending = 0,

        [Description("已看诊")]
        Completed = 1,

        [Description("已取消")]
        Cancelled = 2
    }
}