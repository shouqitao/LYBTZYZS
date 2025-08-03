using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 诊疗状态枚举 - 前后端共享
    /// </summary>
    [Description("诊疗状态")]
    public enum ConsultationStatus
    {
        /// <summary>诊疗中</summary>
        [Description("诊疗中")]
        InProgress = 1,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 2,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 3
    }

    /// <summary>
    /// 病历状态枚举 - 前后端共享
    /// </summary>
    [Description("病历状态")]
    public enum RecordStatus
    {
        /// <summary>进行中</summary>
        [Description("进行中")]
        InProgress = 0,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = 1,

        /// <summary>已共享</summary>
        [Description("已共享")]
        Shared = 2,

        /// <summary>已归档</summary>
        [Description("已归档")]
        Archived = 3
    }
}