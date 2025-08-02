using System.ComponentModel;

namespace LYBT.Shared.Models.Enums {

    /// <summary>
    /// 排队状态枚举 - 前后端共享
    /// </summary>
    [Description("排队状态")]
    public enum QueueStatus {

        /// <summary>等待 - 正在等待叫号</summary>
        [Description("等待")]
        Waiting = 0,

        /// <summary>呼叫中 - 正在呼叫患者</summary>
        [Description("呼叫中")]
        Calling = 1,

        /// <summary>就诊中 - 患者正在就诊</summary>
        [Description("就诊中")]
        InService = 2,

        /// <summary>已完成 - 就诊已完成</summary>
        [Description("已完成")]
        Completed = 3,

        /// <summary>已跳过 - 患者未到跳过</summary>
        [Description("已跳过")]
        Skipped = 4,

        /// <summary>已取消 - 排队被取消</summary>
        [Description("已取消")]
        Cancelled = -1,

        /// <summary>超时 - 等待超时</summary>
        [Description("超时")]
        Timeout = -2
    }
}