using System;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Core.ViewModels.Base
{
    /// <summary>
    /// 对话框ViewModel基类
    /// 提供对话框通用功能：确认/取消、消息框显示等
    /// </summary>
    public abstract class DialogViewModel : ServiceViewModel
    {
        private string _title = string.Empty;
        private bool _isModal = true;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 是否为模态对话框
        /// </summary>
        public bool IsModal
        {
            get => _isModal;
            set => SetProperty(ref _isModal, value);
        }

        /// <summary>
        /// 确认命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; protected set; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; protected set; }

        /// <summary>
        /// 对话框结果
        /// </summary>
        public bool? DialogResult { get; protected set; }

        public DialogViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            InitializeCommands();
        }

        public DialogViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            ConfirmCommand = new DelegateCommand(async () => await ExecuteConfirmAsync(), CanExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel, CanExecuteCancel);
        }

        /// <summary>
        /// 执行确认操作
        /// </summary>
        protected virtual async System.Threading.Tasks.Task ExecuteConfirmAsync()
        {
            try
            {
                IsLoading = true;
                ClearError();
                
                if (await OnConfirmAsync())
                {
                    DialogResult = true;
                    OnDialogCompleted();
                }
            }
            catch (Exception ex)
            {
                HandleError("确认操作", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 子类重写此方法实现具体的确认逻辑
        /// </summary>
        /// <returns>返回true表示操作成功，对话框将关闭</returns>
        protected virtual System.Threading.Tasks.Task<bool> OnConfirmAsync()
        {
            return System.Threading.Tasks.Task.FromResult(true);
        }

        /// <summary>
        /// 执行取消操作
        /// </summary>
        protected virtual void ExecuteCancel()
        {
            if (OnCancel())
            {
                DialogResult = false;
                OnDialogCompleted();
            }
        }

        /// <summary>
        /// 子类重写此方法实现具体的取消逻辑
        /// </summary>
        /// <returns>返回true表示允许取消</returns>
        protected virtual bool OnCancel()
        {
            return true;
        }

        /// <summary>
        /// 对话框完成时调用
        /// </summary>
        protected virtual void OnDialogCompleted()
        {
            // 子类可以重写此方法进行清理或通知
        }

        /// <summary>
        /// 是否可以执行确认操作
        /// </summary>
        protected virtual bool CanExecuteConfirm()
        {
            return !IsLoading && !HasError;
        }

        /// <summary>
        /// 是否可以执行取消操作
        /// </summary>
        protected virtual bool CanExecuteCancel()
        {
            return !IsLoading;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected bool ShowConfirmDialog(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 显示信息对话框
        /// </summary>
        protected void ShowInfoDialog(string message, string title = "信息")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        protected void ShowErrorDialog(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnLoadingStateChanged(bool isLoading)
        {
            base.OnLoadingStateChanged(isLoading);
            ConfirmCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }
    }
}