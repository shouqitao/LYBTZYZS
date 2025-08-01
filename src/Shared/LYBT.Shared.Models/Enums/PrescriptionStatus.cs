using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 处方状态枚举 - 前后端共享
    /// </summary>
    [Description("处方状态")]
    public enum PrescriptionStatus
    {
        /// <summary>草稿 - 处方正在编辑中</summary>
        [Description("草稿")]
        Draft = 0,

        /// <summary>已开具 - 医生已开具处方</summary>
        [Description("已开具")]
        Issued = 1,

        /// <summary>已确认 - 处方已确认可以调配</summary>
        [Description("已确认")]
        Confirmed = 2,

        /// <summary>已调配 - 药房已调配完成</summary>
        [Description("已调配")]
        Dispensed = 3,

        /// <summary>已完成 - 患者已取药</summary>
        [Description("已完成")]
        Completed = 4,

        /// <summary>已取消 - 处方被取消</summary>
        [Description("已取消")]
        Cancelled = -1,

        /// <summary>已作废 - 处方被作废</summary>
        [Description("已作废")]
        Voided = -2
    }
}