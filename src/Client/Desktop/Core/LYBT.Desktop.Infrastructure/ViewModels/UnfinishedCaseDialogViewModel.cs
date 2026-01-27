using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// 未完成医案对话框ViewModel
    /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// OpenSpec: unify-dialog-to-prism - 迁移到Prism DialogService
    /// 支持4个选项：继续看诊、新建医案、仅关闭、取消
    /// </summary>
    public partial class UnfinishedCaseDialogViewModel : ObservableObject, IDialogAware
    {
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

        #region IDialogAware 实现

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            // 对话框关闭时的清理工作（如需要）
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 从参数获取患者姓名
            if (parameters.TryGetValue<string>("PatientName", out var patientName))
            {
                PatientName = patientName;
            }
        }

        #endregion

        /// <summary>继续看诊命令</summary>
        [RelayCommand]
        private void Continue()
        {
            Result = UnfinishedCaseChoice.Continue;
            CloseDialog();
        }

        /// <summary>新建医案命令</summary>
        [RelayCommand]
        private void CreateNew()
        {
            Result = UnfinishedCaseChoice.CloseAndCreate;
            CloseDialog();
        }

        /// <summary>仅关闭命令</summary>
        [RelayCommand]
        private void CloseOnly()
        {
            Result = UnfinishedCaseChoice.CloseOnly;
            CloseDialog();
        }

        /// <summary>取消命令</summary>
        [RelayCommand]
        private void Cancel()
        {
            Result = UnfinishedCaseChoice.Cancel;
            CloseDialog();
        }

        /// <summary>
        /// 关闭对话框并返回结果
        /// </summary>
        private void CloseDialog()
        {
            var parameters = new DialogParameters
            {
                { "Result", Result }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }
    }
}
