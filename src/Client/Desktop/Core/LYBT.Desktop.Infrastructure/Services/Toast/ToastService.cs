using System;
using System.Windows;
using System.Windows.Threading;
using LYBT.Desktop.Infrastructure.Controls.Toast;

namespace LYBT.Desktop.Infrastructure.Services.Toast;

/// <summary>
/// Toast消息服务实现
/// 在窗口顶部显示轻量级消息提示
/// ADR-0003: 使用轻量Toast替代MessageBox和HandyControl Snackbar
/// </summary>
public class ToastService : IToastService
{
    private readonly Dispatcher _dispatcher;
    private ToastControl? _currentToast;

    public ToastService()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    public void ShowInfo(string message)
    {
        Show(message, ToastType.Info);
    }

    /// <summary>
    /// 显示成功消息
    /// </summary>
    public void ShowSuccess(string message)
    {
        Show(message, ToastType.Success);
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    public void ShowWarning(string message)
    {
        Show(message, ToastType.Warning);
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    public void ShowError(string message)
    {
        Show(message, ToastType.Error, 4000); // 错误消息显示更长时间
    }

    /// <summary>
    /// 显示自定义持续时间Toast
    /// </summary>
    public void Show(string message, ToastType type, int durationMilliseconds = 3000)
    {
        _dispatcher.Invoke(() =>
        {
            try
            {
                // 获取主窗口
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null)
                {
                    // 如果没有主窗口，使用MessageBox作为后备
                    ShowMessageBoxFallback(message, type);
                    return;
                }

                // 移除现有Toast
                if (_currentToast != null)
                {
                    mainWindow.UnregisterName("CurrentToast");
                    ((Panel)mainWindow.Content).Children.Remove(_currentToast);
                }

                // 创建新Toast
                _currentToast = new ToastControl
                {
                    Name = "CurrentToast"
                };

                // 设置Toast位置（相对于主窗口）
                var adornerLayer = AdornerLayer.GetOrCreate(mainWindow);
                adornerLayer.Children.Add(_currentToast);

                // 显示Toast
                _currentToast.Visibility = Visibility.Visible;
                _currentToast.Show(message, type, durationMilliseconds);
            }
            catch (Exception)
            {
                // 如果Toast显示失败，使用MessageBox作为后备
                ShowMessageBoxFallback(message, type);
            }
        });
    }

    /// <summary>
    /// MessageBox后备方案（用于Toast不可用时）
    /// </summary>
    private void ShowMessageBoxFallback(string message, ToastType type)
    {
        var icon = type switch
        {
            ToastType.Info => MessageBoxImage.Information,
            ToastType.Success => MessageBoxImage.None,
            ToastType.Warning => MessageBoxImage.Warning,
            ToastType.Error => MessageBoxImage.Error,
            _ => MessageBoxImage.Information
        };

        var title = type switch
        {
            ToastType.Info => "信息",
            ToastType.Success => "成功",
            ToastType.Warning => "警告",
            ToastType.Error => "错误",
            _ => "提示"
        };

        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
    }
}

/// <summary>
/// AdornerLayer辅助类 - 用于在窗口顶部显示Toast
/// </summary>
internal static class AdornerLayer
{
    public static Panel GetOrCreate(Window window)
    {
        // 简化实现：返回窗口的Content面板
        // 实际项目中应该使用AdornerDecorator
        if (window.Content is Panel panel)
        {
            return panel;
        }

        // 如果Content不是Panel，包装它
        var grid = new Grid();
        grid.Children.Add(window.Content);
        window.Content = grid;
        return grid;
    }
}
