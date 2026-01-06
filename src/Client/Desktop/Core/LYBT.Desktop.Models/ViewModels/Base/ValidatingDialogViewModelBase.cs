using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 带验证功能的对话框ViewModel基类
    /// OpenSpec: standardize-viewmodel-framework
    ///
    /// 继承自ValidatingViewModelBase，提供:
    /// - INotifyDataErrorInfo验证支持
    /// - IDialogAware对话框功能
    /// - 消息对话框方法
    /// </summary>
    public abstract partial class ValidatingDialogViewModelBase : ValidatingViewModelBase, IDialogAware
    {
        #region 服务

        /// <summary>
        /// 对话框服务
        /// </summary>
        protected ICommonDialogService? DialogService { get; }

        #endregion

        #region 可观察属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        [ObservableProperty]
        private string _dialogTitle = string.Empty;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数（仅日志和事件）
        /// </summary>
        protected ValidatingDialogViewModelBase(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator)
            : base(loggerFactory, eventAggregator)
        {
        }

        /// <summary>
        /// 构造函数（带对话框服务）
        /// </summary>
        protected ValidatingDialogViewModelBase(
            ILoggerFactory loggerFactory,
            IEventAggregator eventAggregator,
            ICommonDialogService dialogService)
            : base(loggerFactory, eventAggregator)
        {
            DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        }

        #endregion

        #region IDialogAware实现

        /// <summary>
        /// 对话框标题
        /// </summary>
        public virtual string Title => DialogTitle;

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public virtual bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogDebug("对话框已打开: {Type}", GetType().Name);
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public virtual void OnDialogClosed()
        {
            Logger.LogDebug("对话框已关闭: {Type}", GetType().Name);
        }

        #endregion

        #region 对话框辅助方法

        /// <summary>
        /// 关闭对话框
        /// </summary>
        /// <param name="result">对话框结果</param>
        /// <param name="parameters">返回参数</param>
        protected void CloseDialog(ButtonResult result, IDialogParameters? parameters = null)
        {
            RequestClose?.Invoke(new DialogResult(result, parameters ?? new DialogParameters()));
        }

        /// <summary>
        /// 关闭对话框（成功）
        /// </summary>
        protected void CloseDialogOk(IDialogParameters? parameters = null)
        {
            CloseDialog(ButtonResult.OK, parameters);
        }

        /// <summary>
        /// 关闭对话框（取消）
        /// </summary>
        protected void CloseDialogCancel()
        {
            CloseDialog(ButtonResult.Cancel);
        }

        #endregion

        #region 消息对话框方法

        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected virtual async Task ShowSuccessMessageAsync(string message)
        {
            if (DialogService != null)
            {
                await DialogService.ShowInfoAsync(message, "成功");
            }
            else
            {
                Logger.LogInformation("成功: {Message}", message);
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected virtual async Task ShowErrorMessageAsync(string message)
        {
            if (DialogService != null)
            {
                await DialogService.ShowErrorAsync(message, "错误");
            }
            else
            {
                Logger.LogError("错误: {Message}", message);
                ErrorMessage = message;
            }
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        protected virtual async Task ShowWarningMessageAsync(string message)
        {
            if (DialogService != null)
            {
                await DialogService.ShowWarningAsync(message, "警告");
            }
            else
            {
                Logger.LogWarning("警告: {Message}", message);
            }
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected virtual async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            if (DialogService != null)
            {
                return await DialogService.ShowConfirmAsync(message, title);
            }

            Logger.LogWarning("确认对话框不可用，默认返回false: {Message}", message);
            return false;
        }

        #endregion

        #region 状态辅助方法

        /// <summary>
        /// 设置忙碌状态（兼容旧接口）
        /// </summary>
        protected void SetIsBusy(bool isBusy, string? message = null)
        {
            IsBusy = isBusy;
            if (!string.IsNullOrEmpty(message))
            {
                StatusMessage = message;
            }
            else if (!isBusy)
            {
                StatusMessage = string.Empty;
            }
        }

        #endregion
    }
}
