using System.ComponentModel;

namespace LYBT.Common.Enums {

    /// <summary>
    /// 治疗任务状态
    /// </summary>
    [Description("治疗任务状态")]
/// <summary>
/// 表示TreatmentTaskStatus。
/// </summary>
    public enum TreatmentTaskStatus {

        /// <summary>待执行</summary>
        [Description("待执行")]
        Pending = 0,

        /// <summary>进行中</summary>
        [Description("进行中")]
        InProgress = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2
    }
}
