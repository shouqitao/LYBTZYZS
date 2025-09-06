using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Win32;
using Application = System.Windows.Application;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using MessageBox = System.Windows.MessageBox;

namespace LYBT.Desktop.Services {

    /// <summary>
    /// 通用对话框服务实现
    /// </summary>
    public class CommonDialogService : ICommonDialogService {

        #region 消息对话框（异步）

        public Task<bool> ShowConfirmationAsync(string message, string title = "确认") {
            return Task.Run(() => {
                var result = MessageBox.Show(
                    Application.Current.MainWindow,
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                return result == MessageBoxResult.Yes;
            });
        }

        public Task ShowInformationAsync(string message, string title = "信息") {
            return Task.Run(() => {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
        }

        public Task ShowWarningAsync(string message, string title = "警告") {
            return Task.Run(() => {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }

        public Task ShowErrorAsync(string message, string title = "错误") {
            return Task.Run(() => {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }

        #endregion 消息对话框（异步）

        #region 输入对话框

        public Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "") {
            return Task.Run(() => {
                // 由于 WPF 没有内置的输入对话框，这里使用简单的 Windows Forms 实现
                // 在实际项目中，应该创建自定义的 WPF 输入对话框
                return Application.Current.Dispatcher.Invoke(() => {
                    var inputDialog = new System.Windows.Window {
                        /* Title = title, */
                        Width = 400,
                        Height = 200,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Application.Current.MainWindow
                    };

                    var stackPanel = new System.Windows.Controls.StackPanel {
                        Margin = new Thickness(20)
                    };

                    var textBlock = new System.Windows.Controls.TextBlock {
                        Text = message,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap
                    };

                    var textBox = new System.Windows.Controls.TextBox {
                        Text = defaultValue,
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    var buttonPanel = new System.Windows.Controls.StackPanel {
                        Orientation = System.Windows.Controls.Orientation.Horizontal,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                    };

                    var okButton = new System.Windows.Controls.Button {
                        Content = "确定",
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0),
                        IsDefault = true
                    };

                    var cancelButton = new System.Windows.Controls.Button {
                        Content = "取消",
                        Width = 80,
                        Height = 30,
                        IsCancel = true
                    };

                    string? result = null;

                    okButton.Click += (s, e) => {
                        result = textBox.Text;
                        inputDialog.DialogResult = true;
                    };

                    cancelButton.Click += (s, e) => {
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

        public Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "打开文件") {
            return Task.Run(() => {
                return Application.Current.Dispatcher.Invoke(() => {
                    var dialog = new OpenFileDialog {
                        Filter = filter,
                        Title = title
                    };

                    if (dialog.ShowDialog() == true) {
                        return dialog.FileName;
                    }

                    return null;
                });
            });
        }

        public Task<string?> ShowSaveFileDialogAsync(string filter = "All Files (*.*)|*.*", string title = "保存文件", string defaultFileName = "") {
            return Task.Run(() => {
                return Application.Current.Dispatcher.Invoke(() => {
                    var dialog = new SaveFileDialog {
                        Filter = filter,
                        /* Title = title, */
                        FileName = defaultFileName
                    };

                    if (dialog.ShowDialog() == true) {
                        return dialog.FileName;
                    }

                    return null;
                });
            });
        }

        public Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹") {
            return Task.Run(() => {
                return Application.Current.Dispatcher.Invoke(() => {
                    using (var dialog = new FolderBrowserDialog()) {
                        dialog.Description = title;

                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                            return dialog.SelectedPath;
                        }
                    }

                    return null;
                });
            });
        }

        #endregion 文件对话框

        #region 同步方法（为了兼容旧代码）

        public bool ShowConfirmation(string message, string title = "确认") {
            var result = MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public void ShowInformation(string message, string title = "信息") {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "警告") {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "错误") {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion 同步方法（为了兼容旧代码）
    }
}
