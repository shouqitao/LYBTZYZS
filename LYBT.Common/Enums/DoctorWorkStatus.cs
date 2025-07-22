using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 医生在职工作状态
    /// </summary>
    [Description("医生工作状态")]
/// <summary>
/// 表示DoctorWorkStatus。
/// </summary>
    public enum DoctorWorkStatus {

        [Description("诊所坐诊")]
        Clinic = 0,

        [Description("外出就诊")]
        VisitOutside = 1,

        [Description("休假")]
        OnLeave = 2
    }
}
