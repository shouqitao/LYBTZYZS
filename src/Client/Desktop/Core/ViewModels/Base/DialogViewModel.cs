using LYBT.Desktop.Core.Interfaces.Services;
using Prism.Commands;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Base {

    /// <summary>
    /// 对话框ViewModel基类
    /// 提供标准化的保存、取消操作和对话框结果处理
    /// </summary>
    public abstract class DialogViewModel : ServiceViewModel {
        private string _dialogTitle = string.Empty;
        private bool _isSaving;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string DialogTitle {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        /// <summary>
        /// 是否正在保存
        /// </summary>
        public bool IsSaving {
            get => _isSaving;
            protected set {
                SetProperty(ref _isSaving, value);
                SaveCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 保存命令 - 零警告初始化
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 取消命令 - 零警告初始化
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 对话框结果回调
        /// </summary>
        public Action<bool>? DialogResultCallback { get; set; }

        public DialogViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService) {
            // 零警告命令初始化
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel, CanExecuteCancel);
        }

        /// <summary>
        /// 执行保存命令
        /// </summary>
        private async Task ExecuteSaveAsync() {
            try {
                IsSaving = true;
                ClearError();

                var success = await SaveAsync();

                if (success) {
                    OnDialogClosing();
                    DialogResultCallback?.Invoke(true);
                }
            } catch (Exception ex) {
                await HandleErrorAsync("保存", ex);
            } finally {
                IsSaving = false;
            }
        }

        /// <summary>
        /// 执行取消操作
        /// </summary>
        protected virtual void ExecuteCancel() {
            OnDialogClosing();
            DialogResultCallback?.Invoke(false);
        }

        /// <summary>
        /// 执行保存操作 - 子类必须实现
        /// </summary>
        /// <returns>保存是否成功</returns>
        protected abstract Task<bool> SaveAsync();

        /// <summary>
        /// 验证是否可以保存 - 子类可重写
        /// </summary>
        protected virtual bool CanSave() => true;

        /// <summary>
        /// 初始化对话框数据 - 子类可重写
        /// </summary>
        protected virtual void InitializeDialog() { }

        /// <summary>
        /// 对话框关闭前的清理操作 - 子类可重写
        /// </summary>
        protected virtual void OnDialogClosing() { }

        /// <summary>
        /// 判断是否可以执行保存
        /// </summary>
        protected virtual bool CanExecuteSave() {
            return !IsSaving && !IsLoading && CanSave();
        }

        /// <summary>
        /// 判断是否可以执行取消
        /// </summary>
        protected virtual bool CanExecuteCancel() {
            return !IsSaving;
        }

        /// <summary>
        /// 重写Command状态更新
        /// </summary>
        protected override void RaiseCanExecuteChanged() {
            base.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }

        protected override void OnLoadingStateChanged(bool isLoading) {
            base.OnLoadingStateChanged(isLoading);
        }
    }
}
