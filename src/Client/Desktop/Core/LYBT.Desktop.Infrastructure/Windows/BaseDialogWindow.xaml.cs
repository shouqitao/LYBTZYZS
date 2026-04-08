using System.Windows;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Windows;

/// <summary>
/// 基础对话框窗口
/// 统一所有Dialog的样式和行为
/// OpenSpec: unify-dialog-implementation
/// </summary>
public partial class BaseDialogWindow : Window, IDialogWindow
{
    public IDialogResult? Result { get; set; }

    public BaseDialogWindow()
    {
        InitializeComponent();
        
        // 统一对话框样式
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        
        // 默认大小
        Width = 480;
        Height = 320;
        MinWidth = 360;
        MinHeight = 240;
    }

    /// <summary>
    /// 设置对话框内容
    /// </summary>
    public void SetDialogContent(object content)
    {
        if (content is FrameworkElement element)
        {
            // 如果内容有显式的大小设置，调整窗口大小
            if (element.Width > 0)
            {
                Width = element.Width + 40; // 边距
            }
            if (element.Height > 0)
            {
                Height = element.Height + 80; // 边距 + 标题栏
            }
        }
        
        Content = content;
    }
}