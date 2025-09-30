using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LYBT.Desktop.Infrastructure.Controls
{

    /// <summary>
    /// 全局状态栏控件 - 简化版本用于Infrastructure层
    /// </summary>
    public partial class GlobalStatusBar : UserControl, INotifyPropertyChanged
    {

        #region 依赖属性

        /// <summary>
        /// 是否加载中属性
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool),
                typeof(GlobalStatusBar), new PropertyMetadata(false));

        /// <summary>
        /// 加载消息属性
        /// </summary>
        public static readonly DependencyProperty LoadingMessageProperty =
            DependencyProperty.Register(nameof(LoadingMessage), typeof(string),
                typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 状态消息属性
        /// </summary>
        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register(nameof(StatusMessage), typeof(string),
                typeof(GlobalStatusBar), new PropertyMetadata(string.Empty));

        /// <summary>
        /// 操作进度属性
        /// </summary>
        public static readonly DependencyProperty OperationProgressProperty =
            DependencyProperty.Register(nameof(OperationProgress), typeof(int),
                typeof(GlobalStatusBar), new PropertyMetadata(0));

        #endregion 依赖属性

        #region 构造函数

        public GlobalStatusBar()
        {
            InitializeComponent();
            DataContext = this;
        }

        #endregion 构造函数

        #region 公共属性

        /// <summary>是否加载中</summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>加载消息</summary>
        public string LoadingMessage
        {
            get => (string)GetValue(LoadingMessageProperty);
            set => SetValue(LoadingMessageProperty, value);
        }

        /// <summary>状态消息</summary>
        public string StatusMessage
        {
            get => (string)GetValue(StatusMessageProperty);
            set => SetValue(StatusMessageProperty, value);
        }

        /// <summary>操作进度</summary>
        public int OperationProgress
        {
            get => (int)GetValue(OperationProgressProperty);
            set => SetValue(OperationProgressProperty, value);
        }

        #endregion 公共属性

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

        private void OnTimerTick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now;
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}