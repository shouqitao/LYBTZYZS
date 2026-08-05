using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Composition;

/// <summary>
/// 复合 ViewModel 模式中子级 ViewModel 的基类。
/// 提供对父级宿主操作和日志的访问。
/// </summary>
public abstract class ChildViewModelBase : ObservableObject, IDisposable
{
    protected IWorkspaceHost Host { get; }
    protected ILogger Logger { get; }

    protected ChildViewModelBase(IWorkspaceHost host, ILoggerFactory loggerFactory)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        Logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>
    /// 初始化子级 ViewModel（数据加载、订阅等）。
    /// 由父级 ViewModel 在导航生命周期后调用。
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose() { }
}
