using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Foundation.Commands;

/// <summary>
/// 命令工厂 - 提供统一的命令创建模式
/// OpenSpec: refactor-viewmodel-layer Phase 3.1
///
/// 功能:
/// - 创建带加载状态保护的异步命令
/// - 创建带参数的命令
/// - 统一错误处理模式
/// </summary>
public class CommandFactory
{
    private readonly ILogger<CommandFactory> _logger;
    private readonly Func<bool> _getIsBusy;
    private readonly Action<bool> _setIsBusy;
    private readonly Action<Exception, string?>? _errorHandler;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="getIsBusy">获取IsBusy状态的委托</param>
    /// <param name="setIsBusy">设置IsBusy状态的委托</param>
    /// <param name="errorHandler">错误处理委托（可选）</param>
    public CommandFactory(
        ILogger<CommandFactory> logger,
        Func<bool> getIsBusy,
        Action<bool> setIsBusy,
        Action<Exception, string?>? errorHandler = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _getIsBusy = getIsBusy ?? throw new ArgumentNullException(nameof(getIsBusy));
        _setIsBusy = setIsBusy ?? throw new ArgumentNullException(nameof(setIsBusy));
        _errorHandler = errorHandler;
    }

    /// <summary>
    /// 创建带加载状态保护的异步命令
    /// 执行时自动设置IsBusy=true，完成后自动设置IsBusy=false
    /// </summary>
    /// <param name="execute">执行的异步操作</param>
    /// <param name="canExecute">可执行条件（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>DelegateCommand实例</returns>
    public DelegateCommand CreateAsyncWithLoadingGuard(
        Func<Task> execute,
        Func<bool>? canExecute = null,
        string? operationName = null)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));

        return new DelegateCommand(
            async () => await ExecuteWithGuardAsync(execute, operationName),
            () => !_getIsBusy() && (canExecute?.Invoke() ?? true));
    }

    /// <summary>
    /// 创建带参数的异步命令（带加载状态保护）
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="execute">执行的异步操作</param>
    /// <param name="canExecute">可执行条件（可选）</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <returns>DelegateCommand实例</returns>
    public DelegateCommand<T> CreateWithParameter<T>(
        Func<T?, Task> execute,
        Func<T?, bool>? canExecute = null,
        string? operationName = null)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));

        return new DelegateCommand<T>(
            async (param) => await ExecuteWithGuardAsync(() => execute(param), operationName),
            (param) => !_getIsBusy() && (canExecute?.Invoke(param) ?? true));
    }

    /// <summary>
    /// 创建带参数的同步命令
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="execute">执行的同步操作</param>
    /// <param name="canExecute">可执行条件（可选）</param>
    /// <returns>DelegateCommand实例</returns>
    public DelegateCommand<T> CreateSyncWithParameter<T>(
        Action<T?> execute,
        Func<T?, bool>? canExecute = null)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));

        return new DelegateCommand<T>(
            execute,
            canExecute ?? (_ => true));
    }

    /// <summary>
    /// 创建简单的同步命令
    /// </summary>
    /// <param name="execute">执行的同步操作</param>
    /// <param name="canExecute">可执行条件（可选）</param>
    /// <returns>DelegateCommand实例</returns>
    public DelegateCommand CreateSync(
        Action execute,
        Func<bool>? canExecute = null)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));

        return new DelegateCommand(execute, canExecute ?? (() => true));
    }

    /// <summary>
    /// 带保护的异步执行
    /// </summary>
    private async Task ExecuteWithGuardAsync(Func<Task> execute, string? operationName)
    {
        if (_getIsBusy())
        {
            _logger.LogDebug("命令被忽略，当前正忙: {OperationName}", operationName ?? "未命名操作");
            return;
        }

        try
        {
            _setIsBusy(true);
            _logger.LogDebug("开始执行: {OperationName}", operationName ?? "未命名操作");

            await execute().ConfigureAwait(false);

            _logger.LogDebug("执行完成: {OperationName}", operationName ?? "未命名操作");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("操作已取消: {OperationName}", operationName ?? "未命名操作");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行失败: {OperationName}", operationName ?? "未命名操作");
            _errorHandler?.Invoke(ex, operationName);
        }
        finally
        {
            _setIsBusy(false);
        }
    }
}

/// <summary>
/// CommandFactory扩展方法 - 简化ViewModel中的使用
/// </summary>
public static class CommandFactoryExtensions
{
    /// <summary>
    /// 为ViewModel创建CommandFactory实例
    /// </summary>
    /// <param name="loggerFactory">日志工厂</param>
    /// <param name="getIsBusy">获取IsBusy状态的委托</param>
    /// <param name="setIsBusy">设置IsBusy状态的委托</param>
    /// <param name="errorHandler">错误处理委托（可选）</param>
    /// <returns>CommandFactory实例</returns>
    public static CommandFactory CreateCommandFactory(
        this ILoggerFactory loggerFactory,
        Func<bool> getIsBusy,
        Action<bool> setIsBusy,
        Action<Exception, string?>? errorHandler = null)
    {
        var logger = loggerFactory.CreateLogger<CommandFactory>();
        return new CommandFactory(logger, getIsBusy, setIsBusy, errorHandler);
    }
}
