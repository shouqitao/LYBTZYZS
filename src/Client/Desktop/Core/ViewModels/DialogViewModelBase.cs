using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using Prism.Commands;
using Prism.Events;

// using Prism.Dialogs; // Removed for Prism 8.1.97 compatibility

namespace LYBT.Desktop.Core.ViewModels {

    /// <summary>
    /// 对话框视图模型基类
    /// 提供通用的对话框功能
    /// </summary>
    /// <summary>
    /// 对话框ViewModel基类 - UltraThink架构统一
    /// </summary>
    public abstract class DialogViewModelBase : ServiceViewModel {
        private string _title = "对话框";
        private bool? _dialogResult;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult {
            get => _dialogResult;
            protected set => SetProperty(ref _dialogResult, value);
        }

        /// <summary>
        /// 确认命令 - 零警告初始化
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令 - 零警告初始化
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 关闭对话框事件
        /// </summary>
        public event Action<bool?>? RequestClose;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected DialogViewModelBase(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
            ConfirmCommand = new DelegateCommand(async () => await OnConfirmAsync(), CanConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        /// <summary>
        /// 简化构造函数（使用ContainerLocator）
        /// </summary>
        protected DialogViewModelBase() : base(GetEventAggregator(), GetErrorHandlingService()) {
            ConfirmCommand = new DelegateCommand(async () => await OnConfirmAsync(), CanConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        /// <summary>
        /// 确认操作
        /// </summary>
        protected virtual async Task<bool> OnConfirmAsync() {
            try {
                var result = await ExecuteConfirmAsync();
                if (result) {
                    DialogResult = true;
                    RequestClose?.Invoke(true);
                }
                return result;
            } catch (Exception ex) {
                await HandleErrorAsync("确认操作", ex);
                return false;
            }
        }

        /// <summary>
        /// 子类重写此方法实现具体的确认逻辑
        /// </summary>
        protected virtual Task<bool> ExecuteConfirmAsync() => Task.FromResult(true);

        /// <summary>
        /// 取消操作
        /// </summary>
        protected virtual void OnCancel() {
            DialogResult = false;
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// 检查是否可以执行确认操作
        /// </summary>
        protected virtual bool CanConfirm() => !IsLoading;

        /// <summary>
        /// 获取EventAggregator实例
        /// </summary>
        private static IEventAggregator GetEventAggregator() {
            try {
                return (IEventAggregator?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IEventAggregator))
                    ?? new EventAggregator();
            } catch {
                return new EventAggregator();
            }
        }

        /// <summary>
        /// 获取ErrorHandlingService实例
        /// </summary>
        private static IErrorHandlingService GetErrorHandlingService() {
            try {
                return (IErrorHandlingService?)Prism.Ioc.ContainerLocator.Container?.Resolve(typeof(IErrorHandlingService))
                    ?? throw new InvalidOperationException("ErrorHandlingService未注册");
            } catch {
                throw new InvalidOperationException("无法解析ErrorHandlingService");
            }
        }

        /// <summary>
        /// 加载状态变化时更新命令状态
        /// </summary>
        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged() {
            base.RaiseCanExecuteChanged();
            ConfirmCommand?.RaiseCanExecuteChanged();
            CancelCommand?.RaiseCanExecuteChanged();
        }

        protected override void OnLoadingStateChanged(bool isLoading) {
            base.OnLoadingStateChanged(isLoading);
        }
    }
}
