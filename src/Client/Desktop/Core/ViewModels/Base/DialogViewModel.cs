using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Windows.Input;
using System.Reactive.Disposables;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// 对话框视图模型基类
    /// 提供对话框通用功能和属性
    /// </summary>
    public abstract class DialogViewModel : BindableBase, IDisposable
    {
        #region Fields

        private string _title = string.Empty;
        private bool _isBusy = false;
        private string _busyMessage = "处理中...";
        protected readonly IEventAggregator _eventAggregator;
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;

        #endregion

        #region Properties

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
            set => SetProperty(ref _isBusy, value);
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

        #region Commands

        /// <summary>
        /// 确认命令
        /// </summary>
        public ICommand ConfirmCommand { get; protected set; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 初始化对话框视图模型基类
        /// </summary>
        protected DialogViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            InitializeCommands();
        }

        /// <summary>
        /// 无参构造函数（用于设计时支持）
        /// </summary>
        protected DialogViewModel()
        {
            _eventAggregator = new EventAggregator(); // 创建默认实例
            InitializeCommands();
        }

        #endregion

        #region Methods

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
        protected void RaiseCanExecuteChanged()
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

        #region IDisposable

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的核心实现
        /// </summary>
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
        /// 释放时的额外清理工作 - 子类可重写
        /// </summary>
        protected virtual void OnDisposing()
        {
            // 子类可重写以添加额外的清理逻辑
        }

        #endregion
    }

    /// <summary>
    /// 对话框视图模型基类（简化版）
    /// 用于需要对话框功能但不需要事件聚合器的场景
    /// </summary>
    public abstract class DialogViewModelBase : DialogViewModel
    {
        /// <summary>
        /// 初始化简化的对话框视图模型基类
        /// </summary>
        protected DialogViewModelBase() : base()
        {
        }

        /// <summary>
        /// 初始化简化的对话框视图模型基类（带事件聚合器）
        /// </summary>
        protected DialogViewModelBase(IEventAggregator eventAggregator) : base(eventAggregator)
        {
        }
    }
}