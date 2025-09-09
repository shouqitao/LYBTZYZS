using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Services;

namespace LYBT.Desktop.Core.Controls
{

    /// <summary>
    /// 全局状态栏控件 - P7-04 UltraThink用户体验优化
    /// </summary>
    public partial class GlobalStatusBar : UserControl, INotifyPropertyChanged
    {

        #region 依赖属性

        /// <summary>
        /// 用户体验服务属性
        /// </summary>
        public static readonly DependencyProperty UxServiceProperty =
            DependencyProperty.Register(nameof(UxService), typeof(IUserExperienceService),
                typeof(GlobalStatusBar), new PropertyMetadata(null, OnUxServiceChanged));

        #endregion 依赖属性

        #region 私有字段

        private IUserExperienceService? _uxService;

        #endregion 私有字段

        #region 构造函数

        public GlobalStatusBar()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion 构造函数

        #region 公共属性

        /// <summary>用户体验服务</summary>
        public IUserExperienceService? UxService
        {
            get => (IUserExperienceService?)GetValue(UxServiceProperty);
            set => SetValue(UxServiceProperty, value);
        }

        /// <summary>全局加载状态</summary>
        public bool IsGlobalLoading => UxService?.IsGlobalLoading ?? false;

        /// <summary>加载消息</summary>
        public string LoadingMessage => UxService?.LoadingMessage ?? string.Empty;

        /// <summary>状态消息</summary>
        public string StatusMessage => UxService?.StatusMessage ?? string.Empty;

        /// <summary>当前反馈类型</summary>
        public FeedbackType CurrentFeedbackType => UxService?.CurrentFeedbackType ?? FeedbackType.None;

        /// <summary>操作进度</summary>
        public int OperationProgress => UxService?.OperationProgress ?? 0;

        #endregion 公共属性

        #region 私有方法

        /// <summary>用户体验服务变更事件处理</summary>
        private static void OnUxServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GlobalStatusBar statusBar)
            {
                if (e.OldValue is IUserExperienceService oldService)
                {
                    oldService.PropertyChanged -= statusBar.UxService_PropertyChanged;
                }

                if (e.NewValue is IUserExperienceService newService)
                {
                    statusBar._uxService = newService;
                    newService.PropertyChanged += statusBar.UxService_PropertyChanged;
                    statusBar.RefreshAllProperties();
                }
            }
        }

        /// <summary>用户体验服务属性变更事件处理</summary>
        private void UxService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(IUserExperienceService.IsGlobalLoading):
                        OnPropertyChanged(nameof(IsGlobalLoading));
                        break;

                    case nameof(IUserExperienceService.LoadingMessage):
                        OnPropertyChanged(nameof(LoadingMessage));
                        break;

                    case nameof(IUserExperienceService.StatusMessage):
                        OnPropertyChanged(nameof(StatusMessage));
                        break;

                    case nameof(IUserExperienceService.CurrentFeedbackType):
                        OnPropertyChanged(nameof(CurrentFeedbackType));
                        break;

                    case nameof(IUserExperienceService.OperationProgress):
                        OnPropertyChanged(nameof(OperationProgress));
                        break;
                }
            });
        }

        /// <summary>刷新所有属性</summary>
        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(IsGlobalLoading));
            OnPropertyChanged(nameof(LoadingMessage));
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(CurrentFeedbackType));
            OnPropertyChanged(nameof(OperationProgress));
        }

        #endregion 私有方法

        #region INotifyPropertyChanged 实现

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged 实现
    }

    /// <summary>
    /// 反馈类型到可见性转换器
    /// </summary>
    public class FeedbackTypeToVisibilityConverter : IMultiValueConverter
    {
        public static readonly FeedbackTypeToVisibilityConverter Instance = new();

        /// <inheritdoc/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is FeedbackType current &&
                values[1] is string target &&
                Enum.TryParse<FeedbackType>(target, out var targetFeedbackType))
            {
                return current == targetFeedbackType ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <inheritdoc/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 系统时间提供器
    /// </summary>
    public class SystemTimeProvider : INotifyPropertyChanged
    {
        public static readonly SystemTimeProvider Instance = new();

        private readonly DispatcherTimer _timer;
        private DateTime _currentTime;

        private SystemTimeProvider()
        {
            _currentTime = DateTime.Now;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            private set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
                }
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now;
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
