namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 处方药材行显示模型
    /// 用于8列表格布局（每行4种药材）
    /// Issue #1359: [ENTRY-1] 创建PrescriptionItemRow模型
    /// </summary>
    public class PrescriptionItemRow
    {
        /// <summary>
        /// 第1个药材
        /// </summary>
        public PrescriptionItemViewModel? Item1 { get; set; }

        /// <summary>
        /// 第2个药材
        /// </summary>
        public PrescriptionItemViewModel? Item2 { get; set; }

        /// <summary>
        /// 第3个药材
        /// </summary>
        public PrescriptionItemViewModel? Item3 { get; set; }

        /// <summary>
        /// 第4个药材
        /// </summary>
        public PrescriptionItemViewModel? Item4 { get; set; }
    }
}
