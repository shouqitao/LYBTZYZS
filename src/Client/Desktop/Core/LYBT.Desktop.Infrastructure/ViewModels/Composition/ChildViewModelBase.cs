using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.ViewModels.Composition;

/// <summary>
/// Base class for child ViewModels in the Composite VM pattern.
/// Provides access to parent host operations and logging.
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
    /// Initialize the child VM (data loading, subscriptions, etc.).
    /// Called by parent VM after navigation lifecycle.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual void Dispose() { }
}
