using System.ComponentModel;

namespace LYBT.Common.Enums.Patient {

    /// <summary>
    /// 患者状态枚举
    /// </summary>
    [Description("患者状态")]
    public enum PatientStatus {

        [Description("激活")]
        Active = 0,

        [Description("禁用")]
        Disabled = 1
    }
}