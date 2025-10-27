using Prism.Mvvm;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 统一处方药材详细DTO（用于列表模式）
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public class UnifiedPrescriptionItemDto : BindableBase
    {
        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 规格
        /// </summary>
        public string Specification { get; set; } = string.Empty;

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 用法
        /// </summary>
        public string Usage { get; set; } = string.Empty;
    }
}
