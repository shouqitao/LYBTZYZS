using System.Windows.Threading;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Tests.Desktop.Infrastructure.TestDoubles;

public class TestUiThreadDispatcher : IUiThreadDispatcher
{
    public int InvokeCallCount { get; private set; }
    public int InvokeAsyncCallCount { get; private set; }
    public int BeginInvokeCallCount { get; private set; }

    public void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        InvokeCallCount++;
        action();
    }

    public T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        InvokeCallCount++;
        return func();
    }

    public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        InvokeAsyncCallCount++;
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        InvokeAsyncCallCount++;
        return Task.FromResult(func());
    }

    public void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
    {
        BeginInvokeCallCount++;
        action();
    }

    public bool CheckAccess() => true;
}
