using System.ComponentModel;

namespace LYBT.Shared.Models.Enums {

    /// <summary>
    /// 挂号类型枚举 - 前后端共享
    /// </summary>
    [Description("挂号类型")]
    public enum RegistrationType {

        /// <summary>普通号 - 普通门诊</summary>
        [Description("普通号")]
        Regular = 1,

        /// <summary>专家号 - 专家门诊</summary>
        [Description("专家号")]
        Expert = 2,

        /// <summary>急诊号 - 急诊处理</summary>
        [Description("急诊号")]
        Emergency = 3,

        /// <summary>预约号 - 预约就诊</summary>
        [Description("预约号")]
        Appointment = 4
    }

    /// <summary>
    /// 挂号状态枚举 - 前后端共享
    /// </summary>
    [Description("挂号状态")]
    public enum RegistrationStatus {

        /// <summary>已预约 - 患者已预约但未到达</summary>
        [Description("已预约")]
        Scheduled = 0,

        /// <summary>已到达 - 患者已到达候诊</summary>
        [Description("已到达")]
        Arrived = 1,

        /// <summary>就诊中 - 正在就诊</summary>
        [Description("就诊中")]
        InConsultation = 2,

        /// <summary>已完成 - 就诊已完成</summary>
        [Description("已完成")]
        Completed = 3,

        /// <summary>已取消 - 预约被取消</summary>
        [Description("已取消")]
        Cancelled = -1,

        /// <summary>爽约 - 未按时到达</summary>
        [Description("爽约")]
        NoShow = -2,

        /// <summary>已过期 - 预约已过期</summary>
        [Description("已过期")]
        Expired = -3
    }
}