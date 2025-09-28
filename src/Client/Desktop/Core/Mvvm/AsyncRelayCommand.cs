using System.ComponentModel;
using System.Windows.Input;

namespace LYBT.Desktop.Core.Mvvm;

/// <summary>
/// 异步中继命令接口 - 提供异步命令的标准契约
/// </summary>
public interface IAsyncCommand : ICommand
{

    /// <summary>
    /// 获取一个值，指示命令是否正在执行
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    /// 异步执行命令
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>表示异步操作的任务</returns>
    Task ExecuteAsync(object? parameter = null);

    /// <summary>
    /// 取消正在执行的命令
    /// </summary>
    void Cancel();
}

/// <summary>
/// 异步中继命令 - 支持异步操作、进度报告、取消和防重复执行
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供企业级异常处理和状态管理
/// </summary>
/// <param name="execute">异步执行方法</param>
/// <param name="canExecute">可执行条件判断方法（可选）</param>
/// <param name="errorHandler">异常处理方法（可选）</param>
public class AsyncRelayCommand(
    Func<Task> execute,
    Func<bool>? canExecute = null,
    Action<Exception>? errorHandler = null) : ObservableObject, IAsyncCommand
{

    #region 私有字段

    private readonly Func<Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<bool>? _canExecute = canExecute;
    private readonly Action<Exception>? _errorHandler = errorHandler;

    private bool _isExecuting = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private NotifyTaskCompletion? _execution;

    #endregion 私有字段

    #region 事件

    /// <summary>
    /// 当 CanExecute 状态发生变化时引发此事件
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    #endregion 事件

    #region 公共属性

    /// <summary>
    /// 获取一个值，指示命令是否正在执行
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
    /// 获取当前执行任务的通知包装器
    /// </summary>
    public NotifyTaskCompletion? Execution
    {
        get => _execution;
        private set => SetProperty(ref _execution, value);
    }

    /// <summary>
    /// 获取取消令牌，用于取消正在执行的操作
    /// </summary>
    public CancellationToken CancellationToken =>
        _cancellationTokenSource?.Token ?? CancellationToken.None;

    #endregion 公共属性

    #region ICommand实现

    /// <summary>
    /// 确定命令是否可以执行
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>如果命令可以执行则为 true；否则为 false</returns>
    public bool CanExecute(object? parameter)
    {
        return !IsExecuting && (_canExecute?.Invoke() ?? true);
    }

    /// <summary>
    /// 同步执行命令（内部调用异步方法）
    /// </summary>
    /// <param name="parameter">命令参数</param>
    public void Execute(object? parameter)
    {
        // 使用 Fire-and-Forget 模式，避免 async void
        _ = ExecuteInternalAsync(parameter);
    }

    #endregion ICommand实现

    #region IAsyncCommand实现

    /// <summary>
    /// 异步执行命令
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>表示异步操作的任务</returns>
    /// <exception cref="OperationCanceledException">操作被取消时抛出</exception>
    /// <exception cref="InvalidOperationException">命令执行失败时抛出</exception>
    public async Task ExecuteAsync(object? parameter = null)
    {
        await ExecuteInternalAsync(parameter);
    }

    /// <summary>
    /// 取消正在执行的命令
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    #endregion IAsyncCommand实现

    #region 命令状态管理

    /// <summary>
    /// 触发 CanExecuteChanged 事件
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion 命令状态管理

    #region 私有执行方法

    /// <summary>
    /// 内部异步执行实现
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>表示异步操作的任务</returns>
    private async Task ExecuteInternalAsync(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        // 取消之前的操作
        await CancelPreviousOperationAsync();

        // 创建新的取消令牌源
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsExecuting = true;

            // 包装任务以支持通知
            var task = ExecuteWithCancellationAsync();
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
            HandleExecutionError(ex);
        }
        finally
        {
            await CleanupExecutionAsync();
        }
    }

    /// <summary>
    /// 执行用户提供的异步操作，支持取消
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    private async Task ExecuteWithCancellationAsync()
    {
        var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        // 创建一个组合任务，支持取消
        var executeTask = _execute();
        var cancellationTask = Task.Delay(-1, cancellationToken);

        var completedTask = await Task.WhenAny(executeTask, cancellationTask);

        if (completedTask == cancellationTask)
        {
            // 取消任务完成，抛出取消异常
            cancellationToken.ThrowIfCancellationRequested();
        }

        // 等待实际任务完成
        await executeTask;
    }

    /// <summary>
    /// 取消之前的操作
    /// </summary>
    /// <returns>表示异步取消操作的任务</returns>
    private async Task CancelPreviousOperationAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            // 等待短暂时间让取消操作完成
            await Task.Delay(10);

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 处理执行错误
    /// </summary>
    /// <param name="ex">发生的异常</param>
    private void HandleExecutionError(Exception ex)
    {
        try
        {
            _errorHandler?.Invoke(ex);
        }
        catch (Exception handlerException)
        {
            // 如果错误处理器本身出错，记录到调试输出
            System.Diagnostics.Debug.WriteLine($"错误处理器异常: {handlerException.Message}");
        }
    }

    /// <summary>
    /// 清理执行状态
    /// </summary>
    /// <returns>表示异步清理操作的任务</returns>
    private async Task CleanupExecutionAsync()
    {
        IsExecuting = false;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        // 短暂延迟以确保状态更新
        await Task.Delay(1);
    }

    #endregion 私有执行方法
}

/// <summary>
/// 带参数的异步中继命令 - 支持参数化异步操作和进度报告
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// </summary>
/// <typeparam name="T">参数类型</typeparam>
/// <param name="execute">异步执行方法</param>
/// <param name="canExecute">可执行条件判断方法（可选）</param>
/// <param name="errorHandler">异常处理方法（可选）</param>
/// <param name="progress">进度报告器（可选）</param>
public class AsyncRelayCommand<T>(
    Func<T?, Task> execute,
    Predicate<T?>? canExecute = null,
    Action<Exception>? errorHandler = null,
    IProgress<int>? progress = null) : ObservableObject, IAsyncCommand
{

    #region 私有字段

    private readonly Func<T?, Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<T?>? _canExecute = canExecute;
    private readonly Action<Exception>? _errorHandler = errorHandler;
    private readonly IProgress<int>? _progress = progress;

    private bool _isExecuting = false;
    private int _progressValue = 0;
    private CancellationTokenSource? _cancellationTokenSource;
    private NotifyTaskCompletion? _execution;

    #endregion 私有字段

    #region 事件

    /// <summary>
    /// 当 CanExecute 状态发生变化时引发此事件
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    #endregion 事件

    #region 公共属性

    /// <summary>
    /// 获取一个值，指示命令是否正在执行
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
    /// 获取或设置进度值 (0-100)
    /// </summary>
    public int ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    /// <summary>
    /// 获取当前执行任务的通知包装器
    /// </summary>
    public NotifyTaskCompletion? Execution
    {
        get => _execution;
        private set => SetProperty(ref _execution, value);
    }

    /// <summary>
    /// 获取取消令牌，用于取消正在执行的操作
    /// </summary>
    public CancellationToken CancellationToken =>
        _cancellationTokenSource?.Token ?? CancellationToken.None;

    #endregion 公共属性

    #region ICommand实现

    /// <summary>
    /// 确定命令是否可以执行
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>如果命令可以执行则为 true；否则为 false</returns>
    public bool CanExecute(object? parameter)
    {
        return !IsExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);
    }

    /// <summary>
    /// 同步执行命令（内部调用异步方法）
    /// </summary>
    /// <param name="parameter">命令参数</param>
    public void Execute(object? parameter)
    {
        // 使用 Fire-and-Forget 模式，避免 async void
        _ = ExecuteInternalAsync((T?)parameter);
    }

    #endregion ICommand实现

    #region IAsyncCommand实现

    /// <summary>
    /// 异步执行命令
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task ExecuteAsync(object? parameter = null)
    {
        await ExecuteInternalAsync((T?)parameter);
    }

    /// <summary>
    /// 取消正在执行的命令
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

    #endregion IAsyncCommand实现

    #region 类型安全的异步执行

    /// <summary>
    /// 类型安全的异步执行命令
    /// </summary>
    /// <param name="parameter">强类型参数</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task ExecuteAsync(T? parameter = default)
    {
        await ExecuteInternalAsync(parameter);
    }

    #endregion 类型安全的异步执行

    #region 命令状态管理

    /// <summary>
    /// 触发 CanExecuteChanged 事件
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion 命令状态管理

    #region 私有执行方法

    /// <summary>
    /// 内部异步执行实现
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <returns>表示异步操作的任务</returns>
    private async Task ExecuteInternalAsync(T? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        // 取消之前的操作
        await CancelPreviousOperationAsync();

        // 创建新的取消令牌源
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsExecuting = true;
            ProgressValue = 0;

            // 设置进度报告
            var progressReporter = CreateProgressReporter();

            // 包装任务以支持通知
            var task = ExecuteWithProgressAsync(parameter, progressReporter);
            Execution = new NotifyTaskCompletion(task);

            await task;

            ProgressValue = 100;
        }
        catch (OperationCanceledException)
        {
            // 操作被取消，重置进度
            ProgressValue = 0;
        }
        catch (Exception ex)
        {
            // 处理错误，重置进度
            ProgressValue = 0;
            HandleExecutionError(ex);
        }
        finally
        {
            await CleanupExecutionAsync();
        }
    }

    /// <summary>
    /// 执行用户提供的异步操作，支持进度报告
    /// </summary>
    /// <param name="parameter">命令参数</param>
    /// <param name="progressReporter">进度报告器</param>
    /// <returns>表示异步操作的任务</returns>
    private async Task ExecuteWithProgressAsync(T? parameter, IProgress<int>? progressReporter)
    {
        var cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        // 创建一个组合任务，支持取消和进度报告
        var executeTask = _execute(parameter);
        var cancellationTask = Task.Delay(-1, cancellationToken);

        var completedTask = await Task.WhenAny(executeTask, cancellationTask);

        if (completedTask == cancellationTask)
        {
            // 取消任务完成，抛出取消异常
            cancellationToken.ThrowIfCancellationRequested();
        }

        // 等待实际任务完成
        await executeTask;
    }

    /// <summary>
    /// 创建进度报告器
    /// </summary>
    /// <returns>进度报告器实例</returns>
    private IProgress<int>? CreateProgressReporter()
    {
        if (_progress == null)
        {
            return new Progress<int>(value => ProgressValue = Math.Clamp(value, 0, 100));
        }

        return new Progress<int>(value =>
        {
            var clampedValue = Math.Clamp(value, 0, 100);
            ProgressValue = clampedValue;
            _progress.Report(clampedValue);
        });
    }

    /// <summary>
    /// 取消之前的操作
    /// </summary>
    /// <returns>表示异步取消操作的任务</returns>
    private async Task CancelPreviousOperationAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            // 等待短暂时间让取消操作完成
            await Task.Delay(10);

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 处理执行错误
    /// </summary>
    /// <param name="ex">发生的异常</param>
    private void HandleExecutionError(Exception ex)
    {
        try
        {
            _errorHandler?.Invoke(ex);
        }
        catch (Exception handlerException)
        {
            // 如果错误处理器本身出错，记录到调试输出
            System.Diagnostics.Debug.WriteLine($"错误处理器异常: {handlerException.Message}");
        }
    }

    /// <summary>
    /// 清理执行状态
    /// </summary>
    /// <returns>表示异步清理操作的任务</returns>
    private async Task CleanupExecutionAsync()
    {
        IsExecuting = false;

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        // 短暂延迟以确保状态更新
        await Task.Delay(1);
    }

    #endregion 私有执行方法
}

/// <summary>
/// 任务完成通知包装器 - 提供任务状态的可绑定属性
/// </summary>
/// <param name="task">要包装的任务</param>
public class NotifyTaskCompletion(Task task) : INotifyPropertyChanged
{

    /// <summary>
    /// 属性更改事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 获取包装的任务
    /// </summary>
    public Task Task { get; } = task ?? throw new ArgumentNullException(nameof(task));

    /// <summary>
    /// 获取任务的结果（如果是泛型任务）
    /// </summary>
    public TaskStatus Status => Task.Status;

    /// <summary>
    /// 获取一个值，指示任务是否已完成
    /// </summary>
    public bool IsCompleted => Task.IsCompleted;

    /// <summary>
    /// 获取一个值，指示任务是否成功完成
    /// </summary>
    public bool IsCompletedSuccessfully => Task.IsCompletedSuccessfully;

    /// <summary>
    /// 获取一个值，指示任务是否被取消
    /// </summary>
    public bool IsCanceled => Task.IsCanceled;

    /// <summary>
    /// 获取一个值，指示任务是否出现故障
    /// </summary>
    public bool IsFaulted => Task.IsFaulted;

    /// <summary>
    /// 获取任务的异常信息
    /// </summary>
    public AggregateException? Exception => Task.Exception;

    /// <summary>
    /// 获取内部异常
    /// </summary>
    public Exception? InnerException => Exception?.GetBaseException();

    /// <summary>
    /// 获取错误消息
    /// </summary>
    public string? ErrorMessage => InnerException?.Message;

    /// <summary>
    /// 触发属性更改事件
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    protected virtual void OnPropertyChanged(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 命令扩展方法
/// </summary>
public static class CommandExtensions
{

    /// <summary>
    /// 安全地触发 CanExecuteChanged 事件
    /// </summary>
    /// <param name="command">命令实例</param>
    public static void SafeRaiseCanExecuteChanged(this ICommand command)
    {
        if (command is AsyncRelayCommand asyncCommand)
        {
            asyncCommand.RaiseCanExecuteChanged();
        }
        else if (command is System.Windows.Input.ICommand standardCommand)
        {
            // 对于标准命令，可以尝试触发事件
            // 这里可以扩展支持其他命令类型
        }
    }
}
