using System.Windows;
using System.Windows.Threading;
using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Infrastructure.Services
{
    public sealed class WpfUiThreadDispatcher : IUiThreadDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public WpfUiThreadDispatcher()
        {
            _dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("WPF Application not initialized");
        }

        internal WpfUiThreadDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (_dispatcher.CheckAccess())
                action();
            else
                _dispatcher.Invoke(action, priority);
        }

        public T Invoke<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (_dispatcher.CheckAccess())
                return func();

            return _dispatcher.Invoke(func, priority);
        }

        public Task InvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (_dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return _dispatcher.InvokeAsync(action, priority).Task;
        }

        public Task<T> InvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (_dispatcher.CheckAccess())
                return Task.FromResult(func());

            return _dispatcher.InvokeAsync(func, priority).Task;
        }

        public void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            _dispatcher.BeginInvoke(action, priority);
        }

        public bool CheckAccess() => _dispatcher.CheckAccess();
    }
}
