using System.ComponentModel;

namespace LYBT.Common.Enums {
    /// <summary>
    /// 医生状态（是否在职）
    /// </summary>
    public enum DoctorStatus {
        [Description("在职")]
        Active = 0,

        [Description("离职")]
        Inactive = 1
    }
}
