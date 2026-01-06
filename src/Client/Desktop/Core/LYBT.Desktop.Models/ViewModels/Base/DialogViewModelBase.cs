using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 对话框ViewModel基类
    /// OpenSpec: standardize-viewmodel-framework
    ///
    /// 继承CoreViewModelBase，实现IDialogAware:
    /// - 对话框参数处理
    /// - 对话框结果返回
    /// - 关闭请求事件
    /// - 标准取消命令
    /// </summary>
    public abstract partial class DialogViewModelBase : CoreViewModelBase, IDialogAware
    {
        #region 可观察属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;

        #endregion

        #region 计算属性

        /// <summary>
        /// 是否未在加载
        /// </summary>
        public bool IsNotLoading => !IsLoading;

        #endregion

        #region IDialogAware

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public virtual bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public virtual void OnDialogClosed()
        {
            Logger.LogDebug("对话框已关闭: {DialogType}", GetType().Name);
            OnDialogClosedCore();
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogDebug("对话框已打开: {DialogType}, 参数: {@Parameters}",
                GetType().Name,
                parameters?.Keys);
            OnDialogOpenedCore(parameters);
        }

        #endregion

        #region 构造函数

        protected DialogViewModelBase(ILoggerFactory loggerFactory, IEventAggregator eventAggregator)
            : base(loggerFactory, eventAggregator)
        {
        }

        #endregion

        #region 可重写钩子

        /// <summary>
        /// 对话框打开时的核心处理（子类重写）
        /// </summary>
        protected virtual void OnDialogOpenedCore(IDialogParameters? parameters) { }

        /// <summary>
        /// 对话框关闭时的核心处理（子类重写）
        /// </summary>
        protected virtual void OnDialogClosedCore() { }

        #endregion

        #region 关闭方法

        /// <summary>
        /// 关闭对话框（无参数）
        /// </summary>
        /// <param name="result">按钮结果</param>
        protected void CloseDialog(ButtonResult result = ButtonResult.None)
        {
            RequestClose?.Invoke(new DialogResult(result));
        }

        /// <summary>
        /// 关闭对话框（带参数）
        /// </summary>
        /// <param name="parameters">返回参数</param>
        /// <param name="result">按钮结果</param>
        protected void CloseDialog(IDialogParameters parameters, ButtonResult result = ButtonResult.OK)
        {
            RequestClose?.Invoke(new DialogResult(result, parameters));
        }

        /// <summary>
        /// 关闭对话框并返回数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">参数键</param>
        /// <param name="value">参数值</param>
        /// <param name="result">按钮结果</param>
        protected void CloseDialogWithResult<T>(string key, T value, ButtonResult result = ButtonResult.OK)
        {
            var parameters = new DialogParameters { { key, value } };
            CloseDialog(parameters, result);
        }

        #endregion

        #region 参数提取

        /// <summary>
        /// 获取必需的对话框参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameters">对话框参数</param>
        /// <param name="key">参数键</param>
        /// <returns>参数值</returns>
        /// <exception cref="ArgumentException">参数不存在时抛出</exception>
        protected T GetDialogParameter<T>(IDialogParameters parameters, string key)
        {
            if (parameters.TryGetValue(key, out T? value) && value != null)
            {
                return value;
            }

            throw new ArgumentException($"必需的对话框参数 '{key}' 不存在或为null", key);
        }

        /// <summary>
        /// 获取可选的对话框参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameters">对话框参数</param>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值或默认值</returns>
        protected T GetDialogParameter<T>(IDialogParameters parameters, string key, T defaultValue)
        {
            if (parameters.TryGetValue(key, out T? value) && value != null)
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// 尝试获取对话框参数
        /// </summary>
        protected bool TryGetDialogParameter<T>(IDialogParameters parameters, string key, out T? value)
        {
            return parameters.TryGetValue(key, out value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 取消命令
        /// </summary>
        [RelayCommand]
        protected virtual void Cancel()
        {
            Logger.LogDebug("对话框取消: {DialogType}", GetType().Name);
            CloseDialog(ButtonResult.Cancel);
        }

        /// <summary>
        /// 确认命令（子类通常会重写）
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanConfirm))]
        protected virtual void Confirm()
        {
            Logger.LogDebug("对话框确认: {DialogType}", GetType().Name);
            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 是否可以确认（子类可重写）
        /// </summary>
        protected virtual bool CanConfirm() => !IsBusy && !IsLoading;

        #endregion

        #region 属性变更回调

        /// <summary>
        /// IsLoading属性变更时调用（源生成器回调）
        /// </summary>
        partial void OnIsLoadingChanged(bool value)
        {
            OnIsLoadingChangedCore(value);
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 派生类可重写以响应IsLoading变更
        /// </summary>
        protected virtual void OnIsLoadingChangedCore(bool value) { }

        /// <summary>
        /// IsBusy变更时通知Confirm命令
        /// </summary>
        protected override void OnIsBusyChangedCore(bool value)
        {
            base.OnIsBusyChangedCore(value);
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        #endregion
    }
}
