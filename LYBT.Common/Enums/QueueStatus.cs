using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 排队状态枚举，带中文描述
    /// </summary>
    [Description("排队状态")]
    public enum QueueStatus {

        [Description("等待叫号")]
        Waiting = 0,

        [Description("正在就诊")]
        InProgress = 1,

        [Description("已完成")]
        Finished = 2,

        [Description("已取消")]
        Cancelled = 3,

        [Description("已挂起")]
        OnHold = 4
    }
}