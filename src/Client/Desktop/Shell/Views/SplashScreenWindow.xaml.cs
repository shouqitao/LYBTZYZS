using System.Windows;
using System.Windows.Media.Animation;

namespace LYBT.Desktop.Shell.Views;

/// <summary>
/// 启动画面窗口
/// </summary>
public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string status)
    {
        if (Dispatcher.CheckAccess())
            StatusText.Text = status;
        else
            Dispatcher.Invoke(() => StatusText.Text = status);
    }

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

    /// <summary>
    /// 淡出动画后关闭
    /// </summary>
    public void FadeOut()
    {
        if (Dispatcher.CheckAccess())
        {
            var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
            storyboard.Begin(this);
        }
        else
        {
            Dispatcher.Invoke(() =>
            {
                var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
                storyboard.Begin(this);
            });
        }
    }
}
