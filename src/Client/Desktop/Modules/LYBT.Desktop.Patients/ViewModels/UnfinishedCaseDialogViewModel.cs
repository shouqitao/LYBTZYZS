using Prism.Commands;
using Prism.Mvvm;
using System.Windows;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 未完成医案对话框ViewModel
    /// 支持4个选项：继续看诊、新建医案、仅关闭、取消
    /// </summary>
    public class UnfinishedCaseDialogViewModel : BindableBase
    {
        private string _patientName = string.Empty;
        private Window? _dialogWindow;

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => "检测到未完成医案";

        /// <summary>
        /// 对话框消息
        /// </summary>
        public string Message => $"患者【{PatientName}】有未完成的医案，请选择操作：";

        /// <summary>
        /// 用户选择结果
        /// 1=继续看诊, 2=新建医案, 3=仅关闭, 0=取消
        /// </summary>
        public int Result { get; private set; }

        /// <summary>
        /// 继续看诊命令
        /// </summary>
        public DelegateCommand ContinueCommand { get; }

        /// <summary>
        /// 新建医案命令
        /// </summary>
        public DelegateCommand CreateNewCommand { get; }

        /// <summary>
        /// 仅关闭命令
        /// </summary>
        public DelegateCommand CloseOnlyCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        public UnfinishedCaseDialogViewModel()
        {
            ContinueCommand = new DelegateCommand(ExecuteContinue);
            CreateNewCommand = new DelegateCommand(ExecuteCreateNew);
            CloseOnlyCommand = new DelegateCommand(ExecuteCloseOnly);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        /// <summary>
        /// 设置对话框窗口引用
        /// </summary>
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        private void ExecuteContinue()
        {
            Result = 1;
            _dialogWindow?.Close();
        }

        private void ExecuteCreateNew()
        {
            Result = 2;
            _dialogWindow?.Close();
        }

        private void ExecuteCloseOnly()
        {
            Result = 3;
            _dialogWindow?.Close();
        }

        private void ExecuteCancel()
        {
            Result = 0;
            _dialogWindow?.Close();
        }
    }
}
