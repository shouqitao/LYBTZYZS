using Prism.Mvvm;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 统一处方药材行模型（用于8列快速输入布局）
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public class PrescriptionUnifiedItemRow : BindableBase
    {
        public QuickEntryItem Item1 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item2 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item3 { get; set; } = new QuickEntryItem();
        public QuickEntryItem Item4 { get; set; } = new QuickEntryItem();
    }
}
