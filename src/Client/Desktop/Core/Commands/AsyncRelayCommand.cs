using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LYBT.Desktop.Core.Commands
{
    /// <summary>
    /// 异步命令基类 - 第3阶段质量优化
    /// 支持取消、进度报告和错误处理
    /// 遵循异步编程最佳实践，避免死锁
    /// </summary>
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
        bool IsExecuting { get; }
        void Cancel();
    }

    /// <summary>
    /// 异步命令实现
    /// </summary>
    public class AsyncRelayCommand : IAsyncCommand
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public async Task ExecuteAsync()
        {
            if (IsExecuting) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsExecuting = true;
                await _execute(_cancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                IsExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public bool CanExecute()
        {
            return !IsExecuting && _canExecute();
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        #region ICommand Members

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute();
        }

        async void ICommand.Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        #endregion
    }

    /// <summary>
    /// 带参数的异步命令
    /// </summary>
    public class AsyncRelayCommand<T> : IAsyncCommand
    {
        private readonly Func<T?, CancellationToken, Task> _execute;
        private readonly Func<T?, bool> _canExecute;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public AsyncRelayCommand(Func<T?, CancellationToken, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (_ => true);
        }

        public async Task ExecuteAsync()
        {
            await ExecuteAsync(default(T));
        }

        public async Task ExecuteAsync(T? parameter)
        {
            if (IsExecuting) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsExecuting = true;
                await _execute(parameter, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                IsExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public bool CanExecute()
        {
            return CanExecute(default(T));
        }

        public bool CanExecute(T? parameter)
        {
            return !IsExecuting && _canExecute(parameter);
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        #region ICommand Members

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute((T?)parameter);
        }

        async void ICommand.Execute(object? parameter)
        {
            await ExecuteAsync((T?)parameter);
        }

        #endregion
    }

    /// <summary>
    /// 带进度报告的异步命令
    /// </summary>
    public class ProgressAsyncCommand<TProgress> : IAsyncCommand
    {
        private readonly Func<IProgress<TProgress>, CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly Action<TProgress> _progressCallback;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public TProgress? CurrentProgress { get; private set; }

        public ProgressAsyncCommand(
            Func<IProgress<TProgress>, CancellationToken, Task> execute,
            Action<TProgress> progressCallback,
            Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
            _canExecute = canExecute ?? (() => true);
        }

        public async Task ExecuteAsync()
        {
            if (IsExecuting) return;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsExecuting = true;
                
                var progress = new Progress<TProgress>(value =>
                {
                    CurrentProgress = value;
                    _progressCallback(value);
                });

                await _execute(progress, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            finally
            {
                IsExecuting = false;
                CurrentProgress = default;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public bool CanExecute()
        {
            return !IsExecuting && _canExecute();
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }

        #region ICommand Members

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute();
        }

        async void ICommand.Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        #endregion
    }
}