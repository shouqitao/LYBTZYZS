using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 诊疗状态枚举 - 前后端共享
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    [Description("诊疗状态")]
    public enum ConsultationStatus
    {
        /// <summary>等待开始</summary>
        [Description("等待开始")]
        Pending = 0,

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
}
