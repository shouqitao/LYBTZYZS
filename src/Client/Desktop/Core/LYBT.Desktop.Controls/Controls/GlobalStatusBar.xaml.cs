using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace LYBT.Desktop.Controls.Controls
{
    /// <summary>全局状态栏控件</summary>
    public partial class GlobalStatusBar : UserControl
    {
        public GlobalStatusBar() { InitializeComponent(); }

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(GlobalStatusBar), new PropertyMetadata(false));
        public static readonly DependencyProperty LoadingMessageProperty = DependencyProperty.Register(nameof(LoadingMessage), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty OperationProgressProperty = DependencyProperty.Register(nameof(OperationProgress), typeof(int), typeof(GlobalStatusBar), new PropertyMetadata(0));

        /// <summary>当前登录用户名</summary>
        public static readonly DependencyProperty CurrentUserNameProperty = DependencyProperty.Register(
            nameof(CurrentUserName), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));

        /// <summary>应用版本号</summary>
        public static readonly DependencyProperty AppVersionProperty = DependencyProperty.Register(
            nameof(AppVersion), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata("2.0.0"));

        /// <summary>当前连接地址</summary>
        public static readonly DependencyProperty ConnectionUrlProperty = DependencyProperty.Register(
            nameof(ConnectionUrl), typeof(string), typeof(GlobalStatusBar), new PropertyMetadata("http://127.0.0.1:5100"));

        /// <summary>是否连接本地服务</summary>
        public static readonly DependencyProperty IsLocalProperty = DependencyProperty.Register(
            nameof(IsLocal), typeof(bool), typeof(GlobalStatusBar), new PropertyMetadata(false));

        /// <summary>连接命令（接收新URL为参数）</summary>
        public static readonly DependencyProperty ConnectCommandProperty = DependencyProperty.Register(
            nameof(ConnectCommand), typeof(ICommand), typeof(GlobalStatusBar));

        public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
        public string LoadingMessage { get => (string)GetValue(LoadingMessageProperty); set => SetValue(LoadingMessageProperty, value); }
        public string StatusMessage { get => (string)GetValue(StatusMessageProperty); set => SetValue(StatusMessageProperty, value); }
        public int OperationProgress { get => (int)GetValue(OperationProgressProperty); set => SetValue(OperationProgressProperty, value); }
        public string CurrentUserName { get => (string)GetValue(CurrentUserNameProperty); set => SetValue(CurrentUserNameProperty, value ?? string.Empty); }
        public string AppVersion { get => (string)GetValue(AppVersionProperty); set => SetValue(AppVersionProperty, value); }
        public string ConnectionUrl { get => (string)GetValue(ConnectionUrlProperty); set => SetValue(ConnectionUrlProperty, value ?? "http://127.0.0.1:5100"); }
        public bool IsLocal { get => (bool)GetValue(IsLocalProperty); set => SetValue(IsLocalProperty, value); }
        public ICommand ConnectCommand { get => (ICommand)GetValue(ConnectCommandProperty); set => SetValue(ConnectCommandProperty, value); }

        private void OnConnectionStatusClick(object sender, MouseButtonEventArgs e)
        {
            ConnectionPopup.IsOpen = !ConnectionPopup.IsOpen;
        }

        private void OnSelectLocalClick(object sender, RoutedEventArgs e)
        {
            UrlInput.Text = "http://127.0.0.1:5100";
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(url) || ConnectCommand is null)
                return;

            if (ConnectCommand.CanExecute(url))
                ConnectCommand.Execute(url);

            ConnectionPopup.IsOpen = false;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            ConnectionPopup.IsOpen = false;
        }
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
