using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LYBT.Desktop.Infrastructure.Services.Toast;

namespace LYBT.Desktop.Infrastructure.Controls.Toast;

/// <summary>
/// Toast消息控件 - 轻量级消息提示
/// ADR-0003: 使用轻量Toast替代MessageBox和HandyControl Snackbar
/// </summary>
public partial class ToastControl : UserControl
{
    private DispatcherTimer? _hideTimer;

    public ToastControl()
    {
        InitializeComponent();
        DataContext = this;
    }

    #region Message 依赖属性

    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(ToastControl),
            new PropertyMetadata(string.Empty));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    #endregion

    /// <summary>
    /// 显示Toast消息
    /// </summary>
    public void Show(string message, ToastType type, int durationMilliseconds = 3000)
    {
        Message = message;
        SetIcon(type);

        // 播放显示动画
        var showStoryboard = (Storyboard)TryFindResource("ShowAnimation");
        showStoryboard?.Begin();

        // 设置自动隐藏定时器
        _hideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMilliseconds)
        };
        _hideTimer.Tick += (s, e) =>
        {
            Hide();
            _hideTimer?.Stop();
        };
        _hideTimer.Start();
    }

    /// <summary>
    /// 隐藏Toast消息
    /// </summary>
    public void Hide()
    {
        // 播放隐藏动画
        var hideStoryboard = (Storyboard)TryFindResource("HideAnimation");
        hideStoryboard?.Begin();

        // 动画完成后隐藏控件
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        timer.Tick += (s, e) =>
        {
            Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    /// <summary>
    /// 设置图标
    /// </summary>
    private void SetIcon(ToastType type)
    {
        IconText.Text = type switch
        {
            ToastType.Info => "ℹ️",
            ToastType.Success => "✅",
            ToastType.Warning => "⚠️",
            ToastType.Error => "❌",
            _ => "ℹ️"
        };
    }
}
