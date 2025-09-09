using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Events;

using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Base;

/// <summary>
/// 服务视图模型基类 - 为需要服务交互的ViewModel提供基础功能
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供错误处理、异步操作、命令管理等企业级功能
/// </summary>
/// <param name="eventAggregator">事件聚合器，用于模块间通信</param>
/// <param name="errorHandlingService">错误处理服务，用于统一异常处理</param>
public abstract class ServiceViewModel(
    IEventAggregator eventAggregator,
    IErrorHandlingService errorHandlingService) : CoreViewModel(eventAggregator)
{

    #region 受保护字段

    /// <summary>
    /// 错误处理服务实例
    /// </summary>
    protected readonly IErrorHandlingService ErrorHandlingService = errorHandlingService ??
        throw new ArgumentNullException(nameof(errorHandlingService));

    #endregion 受保护字段

    #region 公共命令

    /// <summary>
    /// 刷新命令 - 执行同步刷新操作
    /// </summary>
    public DelegateCommand RefreshCommand { get; private set; }

    /// <summary>
    /// 异步刷新命令 - 执行异步刷新操作
    /// </summary>
    public DelegateCommand RefreshAsyncCommand { get; private set; }

    #endregion 公共命令

    #region 构造函数初始化

    /// <summary>
    /// Initializes static members of the <see cref="ServiceViewModel"/> class.
    /// 初始化命令和基础设置
    /// </summary>
    static ServiceViewModel()
    {
        // 静态初始化可以在这里添加
    }

    /// <summary>
    /// 实例初始化
    /// </summary>
    private void InitializeViewModel()
    {
        RefreshCommand = new DelegateCommand(ExecuteRefreshInternal, CanExecuteRefresh);
        RefreshAsyncCommand = new DelegateCommand(async () => await ExecuteRefreshAsyncInternal(), CanExecuteRefresh);
    }

    #endregion 构造函数初始化

    #region 初始化方法

    /// <summary>
    /// 异步初始化ViewModel
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    /// <exception cref="InvalidOperationException">初始化失败时抛出</exception>
    public virtual async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();
            await OnInitializeAsync();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync("初始化失败", ex, showDialog: false);
            throw new InvalidOperationException($"ViewModel初始化失败: {ex.Message}", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 子类重写此方法实现具体的初始化逻辑
    /// </summary>
    /// <returns>表示异步初始化操作的任务</returns>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    #endregion 初始化方法

    #region API响应处理

    /// <summary>
    /// 处理API服务响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="response">API响应结果</param>
    /// <param name="successMessage">成功时显示的消息（可选）</param>
    /// <param name="showSuccessMessage">是否显示成功消息</param>
    protected void HandleApiResponse<T>(ServiceResult<T> response, string? successMessage = null, bool showSuccessMessage = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccess)
        {
            ClearError();
            if (showSuccessMessage && !string.IsNullOrEmpty(successMessage))
            {
                StatusMessage = successMessage;
            }
        }
        else
        {
            ErrorMessage = response.ErrorMessage ?? "操作失败";
        }
    }

    /// <summary>
    /// 异步处理API服务响应
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="response">API响应结果</param>
    /// <param name="successMessage">成功时显示的消息（可选）</param>
    /// <param name="showSuccessDialog">是否显示成功对话框</param>
    /// <returns>表示异步处理操作的任务</returns>
    protected async Task HandleApiResponseAsync<T>(ServiceResult<T> response, string? successMessage = null, bool showSuccessDialog = false)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.IsSuccess)
        {
            ClearError();
            if (!string.IsNullOrEmpty(successMessage))
            {
                StatusMessage = successMessage;
                if (showSuccessDialog)
                {
                    // 可以扩展为显示成功对话框
                    await Task.Delay(100); // 模拟异步操作
                }
            }
        }
        else
        {
            ErrorMessage = response.ErrorMessage ?? "操作失败";
            var context = CreateErrorContext("API响应处理");
            var ex = new InvalidOperationException(response.ErrorMessage ?? "API调用失败");
            await HandleErrorAsync("API响应处理", ex, showDialog: true);
        }
    }

    #endregion API响应处理

    #region 异常处理

    /// <summary>
    /// 使用错误处理服务处理异常
    /// </summary>
    /// <param name="operation">操作名称</param>
    /// <param name="ex">异常对象</param>
    protected override void HandleError(string operation, Exception ex)
    {
        try
        {
            var context = CreateErrorContext(operation);
            var handledError = ErrorHandlingService.HandleException(ex, context);
            ErrorMessage = handledError.UserMessage;

            if (handledError.Severity >= SharedCommon.ErrorSeverity.Error)
            {
                _ = ErrorHandlingService.ShowErrorAsync(handledError, showDialog: false);
            }
        }
        catch (Exception handlerEx)
        {
            // 如果错误处理器本身失败，回退到基础处理
            base.HandleError(operation, handlerEx);
        }
    }

    /// <summary>
    /// 异步处理异常
    /// </summary>
    /// <param name="operation">操作名称</param>
    /// <param name="ex">异常对象</param>
    /// <param name="showDialog">是否显示错误对话框</param>
    /// <returns>表示异步异常处理操作的任务</returns>
    protected async Task HandleErrorAsync(string operation, Exception ex, bool showDialog = true)
    {
        try
        {
            var context = CreateErrorContext(operation);
            var handledError = await ErrorHandlingService.HandleExceptionAsync(ex, context);
            ErrorMessage = handledError.UserMessage;

            if (showDialog && handledError.RequiresUserAcknowledgment)
            {
                await ErrorHandlingService.ShowErrorAsync(handledError);
            }
        }
        catch (Exception handlerEx)
        {
            // 如果异步错误处理器失败，使用同步处理
            HandleError(operation, handlerEx);
        }
    }

    #endregion 异常处理

    #region 安全执行方法

    /// <summary>
    /// 安全执行异步操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称（用于错误报告）</param>
    /// <param name="showErrorDialog">是否显示错误对话框</param>
    /// <returns>操作结果，如果出错则返回默认值</returns>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? operationName = null, bool showErrorDialog = true)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            IsLoading = true;
            ClearError();
            return await operation();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(operationName ?? "异步操作", ex, showErrorDialog);
            return default;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 使用错误处理服务安全执行操作
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称（用于错误报告）</param>
    /// <param name="showErrorDialog">是否显示错误对话框</param>
    /// <returns>操作是否成功执行</returns>
    protected async Task<bool> ExecuteSafelyAsync(Func<Task> operation, string? operationName = null, bool showErrorDialog = true)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            var context = CreateErrorContext(operationName ?? "安全执行操作");
            return await ErrorHandlingService.ExecuteSafelyAsync(operation, context, showErrorDialog);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(operationName ?? "安全执行操作", ex, showErrorDialog);
            return false;
        }
    }

    /// <summary>
    /// 安全执行无返回值的异步操作
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称（用于错误报告）</param>
    /// <param name="showErrorDialog">是否显示错误对话框</param>
    /// <returns>操作是否成功执行</returns>
    protected async Task<bool> ExecuteVoidAsync(Func<Task> operation, string? operationName = null, bool showErrorDialog = true)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            IsLoading = true;
            ClearError();
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(operationName ?? "异步操作", ex, showErrorDialog);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion 安全执行方法

    #region 命令创建辅助方法

    /// <summary>
    /// 创建异步命令
    /// </summary>
    /// <param name="executeMethod">异步执行方法</param>
    /// <param name="canExecuteMethod">可执行条件判断方法（可选）</param>
    /// <returns>新创建的异步命令</returns>
    protected DelegateCommand CreateAsyncCommand(Func<Task> executeMethod, Func<bool>? canExecuteMethod = null)
    {
        ArgumentNullException.ThrowIfNull(executeMethod);

        return new DelegateCommand(
            async () => await ExecuteVoidAsync(executeMethod, executeMethod.Method.Name),
            canExecuteMethod ?? (() => !IsLoading));
    }

    /// <summary>
    /// 创建带参数的异步命令
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="executeMethod">异步执行方法</param>
    /// <param name="canExecuteMethod">可执行条件判断方法（可选）</param>
    /// <returns>新创建的带参数异步命令</returns>
    protected DelegateCommand<T> CreateAsyncCommand<T>(Func<T?, Task> executeMethod, Func<T?, bool>? canExecuteMethod = null)
    {
        ArgumentNullException.ThrowIfNull(executeMethod);

        return new DelegateCommand<T>(
            async (parameter) => await ExecuteVoidAsync(() => executeMethod(parameter), executeMethod.Method.Name),
            canExecuteMethod ?? ((_) => !IsLoading));
    }

    #endregion 命令创建辅助方法

    #region 错误上下文管理

    /// <summary>
    /// 创建错误上下文
    /// </summary>
    /// <param name="operationName">操作名称</param>
    /// <returns>错误上下文对象</returns>
    protected virtual ErrorContext CreateErrorContext(string operationName)
    {
        var context = new ErrorContext
        {
            OperationName = operationName,
            ModuleName = GetType().Namespace?.Split('.').LastOrDefault() ?? "Unknown",
            ViewName = GetType().Name.Replace("ViewModel", string.Empty),
            Timestamp = DateTime.Now,
            AdditionalData = CreateAdditionalErrorContext()
        };

        OnCreateErrorContext(context);
        return context;
    }

    /// <summary>
    /// 子类可重写此方法添加特定的错误上下文信息
    /// </summary>
    /// <param name="context">错误上下文对象</param>
    protected virtual void OnCreateErrorContext(ErrorContext context)
    {
        // 子类可以重写此方法添加特定信息
    }

    /// <summary>
    /// 创建额外的错误上下文信息
    /// </summary>
    /// <returns>额外的错误上下文信息字典</returns>
    protected virtual Dictionary<string, object> CreateAdditionalErrorContext()
    {
        return new Dictionary<string, object>
        {
            ["IsLoading"] = IsLoading,
            ["HasError"] = HasError,
            ["ViewModelType"] = GetType().Name
        };
    }

    #endregion 错误上下文管理

    #region 命令状态管理

    /// <summary>
    /// 重写命令状态更新
    /// </summary>
    protected override void RaiseCanExecuteChanged()
    {
        base.RaiseCanExecuteChanged();
        RefreshCommand?.RaiseCanExecuteChanged();
        RefreshAsyncCommand?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 加载状态变化时的处理
    /// </summary>
    /// <param name="isLoading">是否正在加载</param>
    protected override void OnLoadingStateChanged(bool isLoading)
    {
        base.OnLoadingStateChanged(isLoading);

        // 刷新命令状态
        RaiseCanExecuteChanged();
    }

    #endregion 命令状态管理

    #region 刷新命令实现

    /// <summary>
    /// 同步刷新操作的内部实现
    /// </summary>
    private void ExecuteRefreshInternal()
    {
        _ = ExecuteRefreshAsyncInternal();
    }

    /// <summary>
    /// 异步刷新操作的内部实现
    /// </summary>
    /// <returns>表示异步刷新操作的任务</returns>
    private async Task ExecuteRefreshAsyncInternal()
    {
        await ExecuteRefreshAsync();
    }

    /// <summary>
    /// 子类可重写的同步刷新方法
    /// </summary>
    protected virtual void ExecuteRefresh()
    {
        _ = ExecuteRefreshAsync();
    }

    /// <summary>
    /// 子类可重写的异步刷新方法
    /// </summary>
    /// <returns>表示异步刷新操作的任务</returns>
    protected virtual async Task ExecuteRefreshAsync()
    {
        await InitializeAsync();
    }

    /// <summary>
    /// 判断是否可以执行刷新操作
    /// </summary>
    /// <returns>如果可以执行刷新则返回 true；否则返回 false</returns>
    protected virtual bool CanExecuteRefresh()
    {
        return !IsLoading;
    }

    #endregion 刷新命令实现
}
