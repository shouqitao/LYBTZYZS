using Prism.Mvvm;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 快速输入药材项（用于8列模式）
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public class QuickEntryItem : BindableBase
    {
        private string _herbName = string.Empty;
        private decimal _quantity;

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName
        {
            get => _herbName;
            set => SetProperty(ref _herbName, value);
        }

        /// <summary>
        /// 用量
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }
    }
}
