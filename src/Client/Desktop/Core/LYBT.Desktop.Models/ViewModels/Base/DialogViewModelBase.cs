using System.Reactive.Disposables;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 对话框视图模型基类 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供对话框核心功能
    /// </summary>
    public abstract class DialogViewModelBase : BindableBase, IDisposable
    {
        #region 字段

        private string _title = string.Empty;
        private bool _isBusy = false;
        private string _busyMessage = "处理中...";
        protected readonly IEventAggregator EventAggregator;
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;

        #endregion

        #region 属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 是否忙碌
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommands();
                }
            }
        }

        /// <summary>
        /// 忙碌消息
        /// </summary>
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; protected set; }

        #endregion

        #region 命令

        /// <summary>
        /// 确认命令
        /// </summary>
        public ICommand ConfirmCommand { get; protected set; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; protected set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化对话框视图模型基类
        /// </summary>
        protected DialogViewModelBase(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            InitializeCommands();
        }

        /// <summary>
        /// 无参构造函数（用于设计时支持）
        /// </summary>
        protected DialogViewModelBase()
        {
            EventAggregator = new EventAggregator();
            InitializeCommands();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 初始化命令
        /// </summary>
        protected virtual void InitializeCommands()
        {
            ConfirmCommand = new DelegateCommand(OnConfirm, CanConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        /// <summary>
        /// 确认操作
        /// </summary>
        protected virtual void OnConfirm()
        {
            DialogResult = true;
            CloseDialog(true);
        }

        /// <summary>
        /// 判断是否可以确认
        /// </summary>
        protected virtual bool CanConfirm()
        {
            return !IsBusy;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        protected virtual void OnCancel()
        {
            DialogResult = false;
            CloseDialog(false);
        }

        /// <summary>
        /// 关闭对话框
        /// </summary>
        protected virtual void CloseDialog(bool? result)
        {
            DialogResult = result;
            // 子类可以重写此方法以提供自定义关闭逻辑
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        protected void RefreshCommands()
        {
            if (ConfirmCommand is DelegateCommand confirmCmd)
            {
                confirmCmd.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 添加需要释放的资源
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _disposables?.Dispose();
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 释放时的额外清理工作
        /// </summary>
        protected virtual void OnDisposing()
        {
            // 子类可重写
        }

        #endregion
    }
}
