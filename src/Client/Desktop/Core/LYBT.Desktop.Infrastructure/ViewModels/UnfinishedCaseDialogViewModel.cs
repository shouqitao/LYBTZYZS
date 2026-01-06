using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// 未完成医案对话框ViewModel
    /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// 支持4个选项：继续看诊、新建医案、仅关闭、取消
    /// </summary>
    public partial class UnfinishedCaseDialogViewModel : ObservableObject
    {
        private Window? _dialogWindow;

        /// <summary>
        /// 患者姓名
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Message))]
        private string _patientName = string.Empty;

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
        /// </summary>
        public UnfinishedCaseChoice Result { get; private set; } = UnfinishedCaseChoice.Cancel;

        /// <summary>
        /// 设置对话框窗口引用
        /// </summary>
        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        /// <summary>继续看诊命令</summary>
        [RelayCommand]
        private void Continue()
        {
            Result = UnfinishedCaseChoice.Continue;
            _dialogWindow?.Close();
        }

        /// <summary>新建医案命令</summary>
        [RelayCommand]
        private void CreateNew()
        {
            Result = UnfinishedCaseChoice.CloseAndCreate;
            _dialogWindow?.Close();
        }

        /// <summary>仅关闭命令</summary>
        [RelayCommand]
        private void CloseOnly()
        {
            Result = UnfinishedCaseChoice.CloseOnly;
            _dialogWindow?.Close();
        }

        /// <summary>取消命令</summary>
        [RelayCommand]
        private void Cancel()
        {
            Result = UnfinishedCaseChoice.Cancel;
            _dialogWindow?.Close();
        }
    }
}
