using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Events;

namespace LYBT.Desktop.Core.ViewModels.Base;
/// <summary>
/// 核心ViewModel基类 - 为所有ViewModel提供基础功能
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供加载状态管理、错误处理、状态消息和命令管理等企业级功能
/// </summary>
/// <param name="eventAggregator">事件聚合器，用于模块间通信</param>
/// <exception cref="ArgumentNullException">当 <paramref name="eventAggregator"/> 为 null 时抛出</exception>
public abstract class CoreViewModel(IEventAggregator eventAggregator) : BindableBase, IDisposable
{
    protected readonly IEventAggregator EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _disposed;

    /// <summary>
    /// 获取或设置一个值，指示是否正在执行异步操作
    /// </summary>
    /// <value>如果正在加载则为 true；否则为 false</value>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnLoadingStateChanged(value);
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 获取或设置当前状态消息
    /// 用于显示给用户的信息反馈
    /// </summary>
    /// <value>状态消息字符串</value>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// 获取或设置一个值，指示是否存在错误
    /// </summary>
    /// <value>如果有错误则为 true；否则为 false</value>
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    /// <summary>
    /// 获取或设置当前错误消息
    /// 设置时会自动更新HasError属性
    /// </summary>
    /// <value>错误消息字符串</value>
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrEmpty(value);
        }
    }

    /// <summary>
    /// 清除错误命令
    /// 用于清除当前的错误状态和消息
    /// </summary>
    public DelegateCommand ClearErrorCommand => _clearErrorCommand ??= new DelegateCommand(ExecuteClearError, CanExecuteClearError);
    
    private DelegateCommand? _clearErrorCommand;

    /// <summary>
    /// 当加载状态发生更改时调用
    /// 子类可以重写此方法以添加自定义逻辑
    /// </summary>
    /// <param name="isLoading">新的加载状态</param>
    protected virtual void OnLoadingStateChanged(bool isLoading)
    {
        // 子类可以重写此方法添加自定义处理
    }

    /// <summary>
    /// 通知所有命令更新其CanExecute状态
    /// 子类应在重写此方法时调用基类方法
    /// </summary>
    protected virtual void RaiseCanExecuteChanged()
    {
        ClearErrorCommand?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 清除当前的错误状态和消息
    /// 将HasError设为false并清空错误消息
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    /// <summary>
    /// 设置状态消息
    /// 用于向用户显示操作进度或结果
    /// </summary>
    /// <param name="message">要显示的状态消息</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="message"/> 为 null 时抛出</exception>
    protected void SetStatus(string message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        StatusMessage = message;
    }

    /// <summary>
    /// 清除当前的状态消息
    /// 将StatusMessage重置为空字符串
    /// </summary>
    protected void ClearStatus()
    {
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// 处理异常并设置相应的错误状态
    /// 子类可以重写此方法以提供更复杂的错误处理逻辑
    /// </summary>
    /// <param name="operation">发生异常的操作名称</param>
    /// <param name="ex">异常对象</param>
    /// <exception cref="ArgumentException">当操作名称为空时抛出</exception>
    /// <exception cref="ArgumentNullException">当异常对象为 null 时抛出</exception>
    protected virtual void HandleError(string operation, Exception ex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation, nameof(operation));
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));
        
        ErrorMessage = $"{operation}: {ex.Message}";
        
        // 使用结构化日志记录
        System.Diagnostics.Debug.WriteLine(
            "[{ViewModelName}] 操作: {Operation} 发生异常: {Exception}", 
            GetType().Name, operation, ex);
    }

    /// <summary>
    /// 安全执行异步操作，自动处理加载状态和异常
    /// 在执行期间设置IsLoading为true，并在完成后恢复
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称，用于错误报告</param>
    /// <returns>表示异步操作的任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="operation"/> 为 null 时抛出</exception>
    protected async Task ExecuteAsync(Func<Task> operation, string? operationName = null)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));
        
        try
        {
            IsLoading = true;
            ClearError();
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            HandleError(operationName ?? "异步操作", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region 命令实现

    /// <summary>
    /// 执行清除错误命令
    /// 清除当前的错误和状态消息
    /// </summary>
    protected virtual void ExecuteClearError()
    {
        ClearError();
        ClearStatus();
    }

    /// <summary>
    /// 判断是否可以执行清除错误命令
    /// </summary>
    /// <returns>如果存在错误则返回 true；否则返回 false</returns>
    protected virtual bool CanExecuteClearError()
    {
        return HasError;
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放由该类使用的资源
    /// </summary>
    /// <param name="disposing">如果为 true，则释放托管和非托管资源；如果为 false，则仅释放非托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                OnDisposing();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// 在对象被释放时调用
    /// 子类可以重写此方法以清理自定义资源
    /// </summary>
    protected virtual void OnDisposing()
    {
        // 子类可以重写此方法进行自定义清理
    }

    /// <summary>
    /// 释放由该对象使用的所有资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}