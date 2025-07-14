using System.ComponentModel;

namespace LYBT.Common.Enums.Logs {

    /// <summary>
    /// 操作对象类型枚举
    /// </summary>
    [Description("操作对象类型")]
    public enum ObjectType {

        /// <summary>
        /// 用户
        /// </summary>
        [Description("用户")]
        User = 1,

        /// <summary>
        /// 患者
        /// </summary>
        [Description("患者")]
        Patient = 2,

        /// <summary>
        /// 病历
        /// </summary>
        [Description("病历")]
        Record = 3,

        /// <summary>
        /// 药方
        /// </summary>
        [Description("药方")]
        Prescription = 4,

        /// <summary>
        /// 费用结算
        /// </summary>
        [Description("费用结算")]
        Billing = 5,

        /// <summary>
        /// 药房
        /// </summary>
        [Description("药房")]
        Pharmacy = 6,

        /// <summary>
        /// 医生
        /// </summary>
        [Description("医生")]
        Doctor = 7,

        /// <summary>
        /// 排队
        /// </summary>
        [Description("排队")]
        Queueing = 8,

        /// <summary>
        /// 诊疗
        /// </summary>
        [Description("诊疗")]
        DiagnosisTreatment = 9,

        /// <summary>
        /// 经验方模板
        /// </summary>
        [Description("经验方模板")]
        FormulaTemplate = 10,

        /// <summary>
        /// 药材
        /// </summary>
        [Description("药材")]
        Herb = 11,

        /// <summary>
        /// 挂号
        /// </summary>
        [Description("挂号")]
        Registration = 12,

        /// <summary>
        /// 系统设置
        /// </summary>
        [Description("系统设置")]
        Settings = 13,

        /// <summary>
        /// 治疗室
        /// </summary>
        [Description("治疗室")]
        TreatmentRoom = 14,

        /// <summary>
        /// 同步
        /// </summary>
        [Description("同步")]
        Sync = 15,

        /// <summary>
        /// 未知
        /// </summary>
        [Description("未知")]
        Unknown = 99
    }
}