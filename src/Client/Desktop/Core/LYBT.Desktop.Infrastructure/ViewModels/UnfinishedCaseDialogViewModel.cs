using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Shared.Enums;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.ViewModels
{
    /// <summary>
    /// 未完成医案对话框ViewModel
    /// 支持4个选项：继续看诊、新建医案、仅关闭、取消
    /// </summary>
    public partial class UnfinishedCaseDialogViewModel : DialogViewModelBase
    {
        /// <summary>
        /// 患者姓名
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Message))]
        private string _patientName = string.Empty;

        /// <summary>
        /// 对话框消息
        /// </summary>
        public string Message => $"患者【{PatientName}】有未完成的医案，请选择操作：";

        /// <summary>
        /// 用户选择结果
        /// </summary>
        public UnfinishedCaseChoice Result { get; private set; } = UnfinishedCaseChoice.Cancel;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UnfinishedCaseDialogViewModel(IViewModelServices services)
            : base(services)
        {
            Title = "检测到未完成医案";
        }

        /// <summary>
        /// 对话框打开时从参数读取患者姓名
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters != null && parameters.TryGetValue<string>("PatientName", out var patientName))
            {
                PatientName = patientName;
            }
        }

        /// <summary>继续看诊命令</summary>
        [RelayCommand]
        private void Continue()
        {
            Result = UnfinishedCaseChoice.Continue;
            CloseWithResult();
        }

        /// <summary>新建医案命令</summary>
        [RelayCommand]
        private void CreateNew()
        {
            Result = UnfinishedCaseChoice.CloseAndCreate;
            CloseWithResult();
        }

        /// <summary>仅关闭命令</summary>
        [RelayCommand]
        private void CloseOnly()
        {
            Result = UnfinishedCaseChoice.CloseOnly;
            CloseWithResult();
        }

        /// <summary>
        /// 取消命令 - 重写基类Cancel以携带Result参数
        /// 注意: 不加[RelayCommand], 复用基类生成的CancelCommand, 通过虚方法分派调用此重写
        /// </summary>
        protected override void Cancel()
        {
            Result = UnfinishedCaseChoice.Cancel;
            CloseWithResult();
        }

        /// <summary>
        /// 关闭对话框并返回Result参数
        /// 使用独立名称避免与基类 protected CloseDialog 重载产生歧义
        /// </summary>
        private void CloseWithResult()
        {
            CloseDialogWithResult("Result", Result, ButtonResult.OK);
        }
    }
}
