using System.Windows;
using LYBT.Desktop.Admin.Prescriptions.ViewModels;

namespace LYBT.Desktop.Admin.Prescriptions.Views
{
    /// <summary>
    /// 患者选择对话框
    /// </summary>
    public partial class PatientSelectionDialog : Window
    {
        public PatientSelectionDialog()
        {
            InitializeComponent();
            
            // 监听ViewModel的DialogResult变化
            DataContextChanged += (s, e) =>
            {
                if (DataContext is PatientSelectionDialogViewModel vm)
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