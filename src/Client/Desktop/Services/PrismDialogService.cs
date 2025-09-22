using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace LYBT.Desktop.Services
{

    /// <summary>
    /// 基于 ICustomDialogService 的通用对话框服务实现 - 简化版本，接口不存在
    /// </summary>
    public class PrismDialogService // : ICommonDialogService // 接口不存在：ICommonDialogService
    {
        public PrismDialogService() // 简化构造函数，移除不存在的接口参数
        {
        }

        #region 消息对话框（异步）

        public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task ShowInformationAsync(string message, string title = "信息")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task ShowWarningAsync(string message, string title = "警告")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        public Task ShowErrorAsync(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        #endregion 消息对话框（异步）

        #region 输入对话框

        public Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "")
        {
            // 暂时使用原有实现，后续可以创建专门的输入对话框
            return Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var inputDialog = new System.Windows.Window
                    {
                        /* Title = title, */
                        Width = 400,
                        Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Application.Current.MainWindow
                    };

                    var stackPanel = new System.Windows.Controls.StackPanel
                    {
                        Margin = new Thickness(20)
                    };

                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = message,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap
                    };

                    var textBox = new System.Windows.Controls.TextBox
                    {
                        Text = defaultValue,
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    var buttonPanel = new System.Windows.Controls.StackPanel
                    {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };

                    var okButton = new System.Windows.Controls.Button
                    {
                        Content = "确定",
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0),
                        IsDefault = true
                    };

                    var cancelButton = new System.Windows.Controls.Button
                    {
                        Content = "取消",
                        Width = 80,
                        Height = 30,
                        IsCancel = true
                    };

                    string? result = null;

                    okButton.Click += (s, e) =>
                    {
                        result = textBox.Text;
                        inputDialog.DialogResult = true;
                    };

                    cancelButton.Click += (s, e) =>
                    {
                        inputDialog.DialogResult = false;
                    };

                    buttonPanel.Children.Add(okButton);
                    buttonPanel.Children.Add(cancelButton);

                    stackPanel.Children.Add(textBlock);
                    stackPanel.Children.Add(textBox);
                    stackPanel.Children.Add(buttonPanel);

                    inputDialog.Content = stackPanel;

                    inputDialog.ShowDialog();

                    return result;
                });
            });
        }

        #endregion 输入对话框

        #region 文件对话框

        public Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "打开文件")
        {
            return Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new OpenFileDialog
                    {
                        Filter = filter,
                        Title = title
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        return dialog.FileName;
                    }

                    return null;
                });
            });
        }

        public Task<string?> ShowSaveFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "保存文件", string defaultFileName = "")
        {
            return Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = filter,
                        /* Title = title, */
                        FileName = defaultFileName
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        return dialog.FileName;
                    }

                    return null;
                });
            });
        }

        public Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹")
        {
            return Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new SaveFileDialog
                    {
                        Title = title,
                        FileName = "选择此文件夹",
                        Filter = "Folder|*.folder",
                        AddExtension = false,
                        OverwritePrompt = false,
                        CheckPathExists = true
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            return System.IO.Path.GetDirectoryName(dialog.FileName);
                        }
                        catch
                        {
                            return null;
                        }
                    }

                    return null;
                });
            });
        }

        #endregion 文件对话框

        #region 同步方法（为了兼容旧代码）- 改为Fire-and-Forget模式

        public bool ShowConfirmation(string message, string title = "确认")
        {
            // 注意：这种同步调用可能导致死锁，建议使用异步版本
            // 在UI线程中调用时需要特别小心
            try
            {
                return ShowConfirmationAsync(message, title).GetAwaiter().GetResult();
            }
            catch
            {
                return false; // 发生异常时返回false作为安全默认值
            }
        }

        public void ShowInformation(string message, string title = "信息")
        {
            _ = ShowInformationAsync(message, title);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            _ = ShowWarningAsync(message, title);
        }

        public void ShowError(string message, string title = "错误")
        {
            _ = ShowErrorAsync(message, title);
        }

        #endregion 同步方法（为了兼容旧代码）- 改为Fire-and-Forget模式
    }
}
