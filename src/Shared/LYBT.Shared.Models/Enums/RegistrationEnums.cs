using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 挂号来源枚举 -- 决定状态流转和权限
    /// Design: registration-module-design.md D2
    /// </summary>
    public enum RegistrationSource
    {
        /// <summary>前台创建 (经过 Waiting 状态)</summary>
        [Description("前台挂号")]
        Receptionist = 0,

        /// <summary>医生直接看诊 (跳过 Waiting，直接 InProgress)</summary>
        [Description("医生看诊")]
        Doctor = 1
    }

    /// <summary>
    /// 挂号状态枚举
    /// 状态机: Waiting -> InProgress -> Completed/Cancelled
    /// Design: registration-module-design.md 状态机章节
    /// </summary>
    public enum RegistrationStatus
    {
        /// <summary>等待中 (仅 Source=Receptionist)</summary>
        [Description("等待中")]
        Waiting = 0,

        /// <summary>接诊中 (医生已从队列选中或直接看诊)</summary>
        [Description("接诊中")]
        InProgress = 1,

        /// <summary>已完成 (医案 Completed 时自动跟随)</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消 (前台手动取消或医生模式自动取消)</summary>
        [Description("已取消")]
        Cancelled = 3
    }
}
