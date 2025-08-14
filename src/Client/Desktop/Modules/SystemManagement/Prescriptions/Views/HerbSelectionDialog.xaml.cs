using System.Windows;
using LYBT.Desktop.Admin.Prescriptions.ViewModels;

namespace LYBT.Desktop.Admin.Prescriptions.Views
{
    /// <summary>
    /// 药材选择对话框
    /// </summary>
    public partial class HerbSelectionDialog : Window
    {
        public HerbSelectionDialog()
        {
            InitializeComponent();
            
            // 监听ViewModel的DialogResult变化
            DataContextChanged += (s, e) =>
            {
                if (DataContext is HerbSelectionDialogViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.DialogResult) && vm.DialogResult.HasValue)
                        {
                            DialogResult = vm.DialogResult;
                            Close();
                        }
                    };
                }
            };
        }
    }
}