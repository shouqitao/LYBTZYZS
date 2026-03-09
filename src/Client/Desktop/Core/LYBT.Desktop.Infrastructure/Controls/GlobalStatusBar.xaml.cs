using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LYBT.Desktop.Infrastructure.Constants;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>全局状态栏控件</summary>
    public partial class GlobalStatusBar : UserControl, INotifyPropertyChanged
    {
        public GlobalStatusBar() { InitializeComponent(); DataContext = this; }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(GlobalStatusBar), new PropertyMetadata(false));
        public static readonly DependencyProperty LoadingMessageProperty = DependencyProperty.Register(nameof(LoadingMessage), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty OperationProgressProperty = DependencyProperty.Register(nameof(OperationProgress), typeof(int), typeof(GlobalStatusBar), new PropertyMetadata(0));

        /// <summary>当前登录用户名 - US-SHELL-007 (CODE-21)</summary>
        public static readonly DependencyProperty CurrentUserNameProperty = DependencyProperty.Register(
            nameof(CurrentUserName), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));

        /// <summary>应用版本号 - US-SHELL-007 (CODE-21)</summary>
        public static readonly DependencyProperty AppVersionProperty = DependencyProperty.Register(
            nameof(AppVersion), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(SystemConstants.ApplicationVersion));

        public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
        public string LoadingMessage { get => (string)GetValue(LoadingMessageProperty); set => SetValue(LoadingMessageProperty, value); }
        public string StatusMessage { get => (string)GetValue(StatusMessageProperty); set => SetValue(StatusMessageProperty, value); }
        public int OperationProgress { get => (int)GetValue(OperationProgressProperty); set => SetValue(OperationProgressProperty, value); }
        public string CurrentUserName { get => (string)GetValue(CurrentUserNameProperty); set => SetValue(CurrentUserNameProperty, value ?? string.Empty); }
        public string AppVersion { get => (string)GetValue(AppVersionProperty); set => SetValue(AppVersionProperty, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>系统时间提供器</summary>
    public class SystemTimeProvider : INotifyPropertyChanged
    {
        public static readonly SystemTimeProvider Instance = new();
        private readonly DispatcherTimer _timer;
        private DateTime _currentTime;

        private SystemTimeProvider()
        {
            _currentTime = DateTime.Now;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => CurrentTime = DateTime.Now;
            _timer.Start();
        }

        public DateTime CurrentTime { get => _currentTime; private set { if (_currentTime != value) { _currentTime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime))); } } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
