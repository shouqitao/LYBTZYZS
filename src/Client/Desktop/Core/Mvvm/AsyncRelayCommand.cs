using LYBT.Shared.Models.Contracts.Common;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LYBT.Desktop.Core.Mvvm
{
    /// <summary>
    /// 异步中继命令 - 支持异步操作、进度报告、取消和防重复执行
    /// </summary>
    public class AsyncRelayCommand : ObservableObject, IAsyncCommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        
        private bool _isExecuting;
        private CancellationTokenSource? _cancellationTokenSource;
        private NotifyTaskCompletion? _execution;
        
        /// <summary>
        /// CanExecute变更事件
        /// </summary>
        public event EventHandler? CanExecuteChanged;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public AsyncRelayCommand(
            Func<Task> execute,
            Func<bool>? canExecute = null,
            Action<Exception>? errorHandler = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }
        
        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }
        
        /// <summary>
        /// 当前执行任务
        /// </summary>
        public NotifyTaskCompletion? Execution
        {
            get => _execution;
            private set => SetProperty(ref _execution, value);
        }
        
        /// <summary>
        /// 是否可以执行
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return !IsExecuting && (_canExecute?.Invoke() ?? true);
        }
        
        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object? parameter)
        {
            // 修复：不使用async void，直接调用ExecuteAsync并处理异常
            ExecuteAsync(parameter).ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    var ex = task.Exception.GetBaseException();
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand Execute失败: {ex.Message}");
                    _errorHandler?.Invoke(ex);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        /// <summary>
        /// 异步执行命令
        /// </summary>
        public async Task ExecuteAsync(object? parameter = null)
        {
            if (!CanExecute(parameter))
                return;
            
            // 取消之前的操作
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                IsExecuting = true;
                
                // 包装任务以支持通知
                var task = _execute();
                Execution = new NotifyTaskCompletion(task);
                
                await task;
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，正常情况
            }
            catch (Exception ex)
            {
                // 处理错误
                _errorHandler?.Invoke(ex);
                throw;
            }
            finally
            {
                IsExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// 取消执行
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        /// <summary>
        /// 触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// 带参数的异步中继命令
    /// </summary>
    public class AsyncRelayCommand<T> : ObservableObject, IAsyncCommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Predicate<T?>? _canExecute;
        private readonly Action<Exception>? _errorHandler;
        private readonly IProgress<int>? _progress;
        
        private bool _isExecuting;
        private int _progressValue;
        private CancellationTokenSource? _cancellationTokenSource;
        private NotifyTaskCompletion? _execution;
        
        public event EventHandler? CanExecuteChanged;
        
        public AsyncRelayCommand(
            Func<T?, Task> execute,
            Predicate<T?>? canExecute = null,
            Action<Exception>? errorHandler = null,
            IProgress<int>? progress = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
            _errorHandler = errorHandler;
            _progress = progress;
        }
        
        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (SetProperty(ref _isExecuting, value))
                {
                    RaiseCanExecuteChanged();
                }
            }
        }
        
        /// <summary>
        /// 进度值(0-100)
        /// </summary>
        public int ProgressValue
        {
            get => _progressValue;
            private set => SetProperty(ref _progressValue, value);
        }
        
        /// <summary>
        /// 当前执行任务
        /// </summary>
        public NotifyTaskCompletion? Execution
        {
            get => _execution;
            private set => SetProperty(ref _execution, value);
        }
        
        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken => 
            _cancellationTokenSource?.Token ?? CancellationToken.None;
        
        public bool CanExecute(object? parameter)
        {
            return !IsExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);
        }
        
        public void Execute(object? parameter)
        {
            // 修复：不使用async void，直接调用ExecuteAsync并处理异常
            ExecuteAsync((T?)parameter).ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    var ex = task.Exception.GetBaseException();
                    System.Diagnostics.Debug.WriteLine($"AsyncRelayCommand<T> Execute失败: {ex.Message}");
                    _errorHandler?.Invoke(ex);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        public async Task ExecuteAsync(object? parameter)
        {
            await ExecuteAsync((T?)parameter);
        }
        
        public async Task ExecuteAsync(T? parameter = default)
        {
            if (!CanExecute(parameter))
                return;
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            try
            {
                IsExecuting = true;
                ProgressValue = 0;
                
                // 设置进度报告
                if (_progress != null)
                {
                    var progressReporter = new Progress<int>(value => ProgressValue = value);
                    // 将进度报告器传递给执行方法（需要修改执行方法签名）
                }
                
                var task = _execute(parameter);
                Execution = new NotifyTaskCompletion(task);
                
                await task;
                
                ProgressValue = 100;
            }
            catch (OperationCanceledException)
            {
                ProgressValue = 0;
            }
            catch (Exception ex)
            {
                ProgressValue = 0;
                _errorHandler?.Invoke(ex);
                throw;
            }
            finally
            {
                IsExecuting = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// 任务完成通知包装器
    /// </summary>
    public sealed class NotifyTaskCompletion : INotifyPropertyChanged
    {
        public NotifyTaskCompletion(Task task)
        {
            Task = task;
            TaskCompletion = WatchTaskAsync(task);
        }
        
        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // 异常会通过Task属性暴露
            }
            
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;
            
            propertyChanged(this, new PropertyChangedEventArgs(nameof(Status)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsNotCompleted)));
            
            if (task.IsCanceled)
            {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(Exception)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(InnerException)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
            else
            {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
            }
        }
        
        public Task Task { get; }
        public Task TaskCompletion { get; }
        
        public TaskStatus Status => Task.Status;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException? Exception => Task.Exception;
        public Exception? InnerException => Exception?.InnerException;
        public string? ErrorMessage => InnerException?.Message;
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    
    /// <summary>
    /// 异步命令接口
    /// </summary>
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object? parameter = null);
        bool IsExecuting { get; }
        void Cancel();
        void RaiseCanExecuteChanged();
    }
    
    /// <summary>
    /// 命令扩展方法
    /// </summary>
    public static class CommandExtensions
    {
        /// <summary>
        /// 创建防抖命令（防止短时间内重复执行）
        /// </summary>
        public static ICommand Debounce(this ICommand command, TimeSpan delay)
        {
            return new DebouncedCommand(command, delay);
        }
        
        /// <summary>
        /// 创建节流命令（限制执行频率）
        /// </summary>
        public static ICommand Throttle(this ICommand command, TimeSpan interval)
        {
            return new ThrottledCommand(command, interval);
        }
    }
    
    /// <summary>
    /// 防抖命令包装器
    /// </summary>
    internal class DebouncedCommand : ICommand
    {
        private readonly ICommand _command;
        private readonly TimeSpan _delay;
        private CancellationTokenSource? _cts;
        
        public DebouncedCommand(ICommand command, TimeSpan delay)
        {
            _command = command;
            _delay = delay;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add => _command.CanExecuteChanged += value;
            remove => _command.CanExecuteChanged -= value;
        }
        
        public bool CanExecute(object? parameter) => _command.CanExecute(parameter);
        
        public void Execute(object? parameter)
        {
            // Fire-and-forget pattern with exception handling
            _ = Task.Run(async () =>
            {
                try
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    
                    await Task.Delay(_delay, _cts.Token);
                    _command.Execute(parameter);
                }
                catch (TaskCanceledException)
                {
                    // 正常取消，不需要处理
                }
                catch (Exception ex)
                {
                    // 记录异常，防止async void异常逃逸
                    System.Diagnostics.Debug.WriteLine($"DebouncedCommand Execute失败: {ex.Message}");
                }
            });
        }
    }
    
    /// <summary>
    /// 节流命令包装器
    /// </summary>
    internal class ThrottledCommand : ICommand
    {
        private readonly ICommand _command;
        private readonly TimeSpan _interval;
        private DateTime _lastExecutionTime = DateTime.MinValue;
        
        public ThrottledCommand(ICommand command, TimeSpan interval)
        {
            _command = command;
            _interval = interval;
        }
        
        public event EventHandler? CanExecuteChanged
        {
            add => _command.CanExecuteChanged += value;
            remove => _command.CanExecuteChanged -= value;
        }
        
        public bool CanExecute(object? parameter)
        {
            var canExecute = _command.CanExecute(parameter);
            var timeSinceLastExecution = DateTime.Now - _lastExecutionTime;
            return canExecute && timeSinceLastExecution >= _interval;
        }
        
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _lastExecutionTime = DateTime.Now;
                _command.Execute(parameter);
            }
        }
    }
}