using System.Windows.Controls;

namespace LYBT.Desktop.Shell.Dialogs.Views;

/// <summary>
/// ApiConnectionFailedDialog.xaml 的交互逻辑
/// enhance-shell-connection-dialog: API连接失败恢复对话框
/// 注意: Prism对话框必须使用UserControl，不能使用Window
/// </summary>
public partial class ApiConnectionFailedDialog : UserControl
{
    public ApiConnectionFailedDialog()
    {
        InitializeComponent();
    }
}
