using System.Windows;

namespace LYBT.Desktop.Shell.Views;

/// <summary>
/// 启动画面窗口
/// 在应用程序启动时显示加载进度和状态
/// </summary>
public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 更新加载状态文本
    /// </summary>
    /// <param name="status">状态消息</param>
    public void UpdateStatus(string status)
    {
        if (Dispatcher.CheckAccess())
        {
            StatusText.Text = status;
        }
        else
        {
            Dispatcher.Invoke(() => StatusText.Text = status);
        }
    }

    /// <summary>
    /// 更新进度条值
    /// </summary>
    /// <param name="value">进度值 (0-100)</param>
    public void UpdateProgress(double value)
    {
        if (Dispatcher.CheckAccess())
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = value;
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = value;
            });
        }
    }
}
