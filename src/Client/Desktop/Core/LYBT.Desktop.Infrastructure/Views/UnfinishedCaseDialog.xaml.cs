using System.Windows;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels;

namespace LYBT.Desktop.Infrastructure.Views
{
    /// <summary>
    /// UnfinishedCaseDialog.xaml 的交互逻辑
    /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
    /// </summary>
    public partial class UnfinishedCaseDialog : Window
    {
        public UnfinishedCaseDialog()
        {
            InitializeComponent();

            // 将窗口引用传递给ViewModel
            if (DataContext is UnfinishedCaseDialogViewModel vm)
            {
                vm.SetDialogWindow(this);
            }
        }

        /// <summary>
        /// 获取用户选择结果
        /// </summary>
        public UnfinishedCaseChoice Result
        {
            get
            {
                if (DataContext is UnfinishedCaseDialogViewModel vm)
                {
                    return vm.Result;
                }
                return UnfinishedCaseChoice.Cancel;
            }
        }

        /// <summary>
        /// 设置患者姓名
        /// </summary>
        public void SetPatientName(string patientName)
        {
            if (DataContext is UnfinishedCaseDialogViewModel vm)
            {
                vm.PatientName = patientName;
            }
        }
    }
}
