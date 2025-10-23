using LYBT.Desktop.Patients.ViewModels;
using System.Windows;

namespace LYBT.Desktop.Patients.Views
{
    /// <summary>
    /// UnfinishedCaseDialog.xaml 的交互逻辑
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
        public int Result
        {
            get
            {
                if (DataContext is UnfinishedCaseDialogViewModel vm)
                {
                    return vm.Result;
                }
                return 0; // 默认返回取消
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
