using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LYBT.Desktop.Controls.Controls
{
    /// <summary>
    /// 加载遮罩控件
    ///
    /// 功能：
    /// - 半透明遮罩层
    /// - 加载进度指示器
    /// - 可自定义加载文本
    /// - 延迟显示：加载超过 200ms 才显示遮罩，避免快速加载时的闪烁
    /// </summary>
    public partial class LoadingOverlay : UserControl
    {
        private DispatcherTimer? _delayTimer;
        private const int DisplayDelayMs = 200;

        public LoadingOverlay()
        {
            InitializeComponent();
            _delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DisplayDelayMs) };
            _delayTimer.Tick += OnDelayTimerTick;
        }

        #region IsLoading - 是否加载中

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false, OnIsLoadingChanged));

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LoadingOverlay)d).OnIsLoadingChanged((bool)e.NewValue);
        }

        private void OnIsLoadingChanged(bool isLoading)
        {
            if (isLoading)
            {
                _delayTimer?.Start();
            }
            else
            {
                _delayTimer?.Stop();
                IsOverlayVisible = false;
            }
        }

        private void OnDelayTimerTick(object? sender, EventArgs e)
        {
            _delayTimer!.Stop();
            IsOverlayVisible = true;
        }

        #endregion

        #region IsOverlayVisible - 遮罩实际可见性（延迟后）

        public bool IsOverlayVisible
        {
            get => (bool)GetValue(IsOverlayVisibleProperty);
            set => SetValue(IsOverlayVisibleProperty, value);
        }

        public static readonly DependencyProperty IsOverlayVisibleProperty =
            DependencyProperty.Register(nameof(IsOverlayVisible), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false));

        #endregion

        #region LoadingText - 加载文本

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata("正在加载..."));

        #endregion
    }
}
