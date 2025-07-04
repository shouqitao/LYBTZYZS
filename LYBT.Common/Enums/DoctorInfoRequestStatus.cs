using System.ComponentModel;

namespace LYBT.Common.Enums {
    /// <summary>
    /// 医生信息申请状态
    /// </summary>
    public enum DoctorInfoRequestStatus {
        [Description("待审核")]
        Pending = 0,
        [Description("已批准")]
        Approved = 1,
        [Description("已驳回")]
        Rejected = 2
    }
}
