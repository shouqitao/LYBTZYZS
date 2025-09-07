using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels
{

    /// <summary>
    /// UltraThink Phase 3.1: 现代化对话框ViewModel基类
    ///
    /// 专门针对对话框场景优化:
    /// 1. 标准确认/取消Command
    /// 2. 对话框结果管理
    /// 3. 关闭事件处理
    /// 4. 零DelegateCommand警告
    /// </summary>
    public abstract class ModernDialogViewModel : ModernViewModelBase
    {

        #region 对话框专属属性

        private string _title = "对话框";
        private bool? _dialogResult;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 对话框结果（true=确认, false=取消, null=未设置）
        /// </summary>
        public bool? DialogResult
        {
            get => _dialogResult;
            protected set => SetProperty(ref _dialogResult, value);
        }

        #endregion 对话框专属属性

        #region 对话框专属Command

        /// <summary>
        /// 确认命令 - 零警告初始化
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令 - 零警告初始化
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion 对话框专属Command

        #region 对话框事件

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<bool?>? RequestClose;

        #endregion 对话框事件

        #region 构造函数

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernDialogViewModel"/> class.
        /// 标准构造函数
        /// </summary>
        protected ModernDialogViewModel(
            IEventAggregator eventAggregator,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, errorHandlingService)
        {
            // 零警告Command初始化
            ConfirmCommand = new DelegateCommand(async () => await OnConfirmAsync(), CanConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernDialogViewModel"/> class.
        /// 兼容性构造函数
        /// </summary>
        protected ModernDialogViewModel(IEventAggregator eventAggregator)
            : this(eventAggregator, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModernDialogViewModel"/> class.
        /// 简化构造函数
        /// </summary>
        protected ModernDialogViewModel()
            : this(new EventAggregator(), null)
        {
        }

        #endregion 构造函数

        #region 虚方法（子类重写）

        /// <summary>
        /// 确认逻辑 - 子类重写实现具体业务
        /// </summary>
        /// <returns>true=允许关闭, false=阻止关闭</returns>
        protected virtual Task<bool> ExecuteConfirmAsync() => Task.FromResult(true);

        /// <summary>
        /// 检查是否可以确认 - 子类可重写
        /// </summary>
        protected virtual bool CanConfirm() => !IsLoading;

        /// <summary>
        /// 取消逻辑 - 子类可重写
        /// </summary>
        protected virtual void ExecuteCancel()
        {
            // 默认实现：直接关闭
        }

        #endregion 虚方法（子类重写）

        #region Command实现

        /// <summary>
        /// 确认命令执行
        /// </summary>
        private async Task OnConfirmAsync()
        {
            try
            {
                var result = await ExecuteConfirmAsync();
                if (result)
                {
                    DialogResult = true;
                    RequestClose?.Invoke(true);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("确认操作", ex);
            }
        }

        /// <summary>
        /// 取消命令执行
        /// </summary>
        private void OnCancel()
        {
            try
            {
                ExecuteCancel();
                DialogResult = false;
                RequestClose?.Invoke(false);
            }
            catch (Exception ex)
            {
                // 取消操作异常处理
                _ = HandleErrorAsync("取消操作", ex, false);
                DialogResult = false;
                RequestClose?.Invoke(false);
            }
        }

        #endregion Command实现

        #region 重写基类方法

        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
            // CancelCommand通常总是可用，不需要更新
        }

        /// <summary>
        /// 重写加载状态变化处理
        /// </summary>
        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            // 加载时通常禁用确认，但允许取消
        }

        #endregion 重写基类方法

        #region 便捷方法

        /// <summary>
        /// 设置对话框标题（链式调用）
        /// </summary>
        protected ModernDialogViewModel WithTitle(string title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        /// 成功关闭对话框
        /// </summary>
        protected void CloseWithSuccess()
        {
            DialogResult = true;
            RequestClose?.Invoke(true);
        }

        /// <summary>
        /// 取消关闭对话框
        /// </summary>
        protected void CloseWithCancel()
        {
            DialogResult = false;
            RequestClose?.Invoke(false);
        }

        #endregion 便捷方法
    }
}
