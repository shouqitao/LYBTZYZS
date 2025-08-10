using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Exceptions;

namespace LYBT.WPF.Client.Core.ViewModels
{
    /// <summary>
    /// 错误通知视图模型
    /// </summary>
    public class ErrorNotificationViewModel : BindableBase
    {
        private HandledError? _handledError;
        private bool _isVisible;
        private string _userMessage = string.Empty;
        private ErrorSeverity _severity = ErrorSeverity.Error;
        private bool _canRetry;
        private List<string> _suggestedActions = new List<string>();

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string UserMessage
        {
            get => _userMessage;
            set => SetProperty(ref _userMessage, value);
        }

        public ErrorSeverity Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        public bool CanRetry
        {
            get => _canRetry;
            set => SetProperty(ref _canRetry, value);
        }

        public List<string> SuggestedActions
        {
            get => _suggestedActions;
            set
            {
                SetProperty(ref _suggestedActions, value);
                RaisePropertyChanged(nameof(HasSuggestedActions));
            }
        }

        public bool HasSuggestedActions => SuggestedActions?.Any() == true;

        public HandledError? HandledError
        {
            get => _handledError;
            set
            {
                SetProperty(ref _handledError, value);
                UpdateFromHandledError();
            }
        }

        // 命令
        public DelegateCommand CloseCommand { get; }
        public DelegateCommand RetryCommand { get; }
        public DelegateCommand ShowDetailsCommand { get; }

        // 事件
        public event EventHandler? CloseRequested;
        public event EventHandler? RetryRequested;
        public event EventHandler<HandledError>? ShowDetailsRequested;

        public ErrorNotificationViewModel()
        {
            CloseCommand = new DelegateCommand(ExecuteClose);
            RetryCommand = new DelegateCommand(ExecuteRetry, CanExecuteRetry);
            ShowDetailsCommand = new DelegateCommand(ExecuteShowDetails, CanExecuteShowDetails);
        }

        /// <summary>
        /// 显示错误
        /// </summary>
        public void ShowError(HandledError handledError)
        {
            HandledError = handledError;
            IsVisible = true;
        }

        /// <summary>
        /// 隐藏错误通知
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            HandledError = null;
        }

        private void UpdateFromHandledError()
        {
            if (_handledError != null)
            {
                UserMessage = _handledError.UserMessage;
                Severity = _handledError.Severity;
                CanRetry = _handledError.CanRetry;
                SuggestedActions = _handledError.SuggestedActions?.Take(3).ToList() ?? new List<string>();
            }
            else
            {
                UserMessage = string.Empty;
                Severity = ErrorSeverity.Error;
                CanRetry = false;
                SuggestedActions = new List<string>();
            }

            // 刷新命令状态
            RetryCommand.RaiseCanExecuteChanged();
            ShowDetailsCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteClose()
        {
            Hide();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteRetry()
        {
            RetryRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteRetry()
        {
            return CanRetry && HandledError != null;
        }

        private void ExecuteShowDetails()
        {
            if (HandledError != null)
            {
                ShowDetailsRequested?.Invoke(this, HandledError);
            }
        }

        private bool CanExecuteShowDetails()
        {
            return HandledError != null && !string.IsNullOrEmpty(HandledError.TechnicalDetails);
        }
    }
}