using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Prism.Dialogs;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Enums;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 基于 Prism IDialogService 的通用对话框服务实现
    /// </summary>
    public class PrismDialogService : ICommonDialogService
    {
        private readonly IDialogService _dialogService;

        public PrismDialogService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        #region 消息对话框（异步）

        public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new DialogParameters
                {
                    { "title", title },
                    { "message", message }
                };

                _dialogService.ShowDialog("ConfirmationDialog", parameters, result =>
                {
                    tcs.SetResult(result.Result == ButtonResult.Yes);
                });
            });

            return tcs.Task;
        }

        public Task ShowInformationAsync(string message, string title = "信息")
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new DialogParameters
                {
                    { "title", title },
                    { "message", message },
                    { "type", DialogType.Information }
                };

                _dialogService.ShowDialog("InformationDialog", parameters, result =>
                {
                    tcs.SetResult(true);
                });
            });

            return tcs.Task;
        }

        public Task ShowWarningAsync(string message, string title = "警告")
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new DialogParameters
                {
                    { "title", title },
                    { "message", message },
                    { "type", DialogType.Warning }
                };

                _dialogService.ShowDialog("InformationDialog", parameters, result =>
                {
                    tcs.SetResult(true);
                });
            });

            return tcs.Task;
        }

        public Task ShowErrorAsync(string message, string title = "错误")
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var parameters = new DialogParameters
                {
                    { "title", title },
                    { "message", message },
                    { "type", DialogType.Error }
                };

                _dialogService.ShowDialog("InformationDialog", parameters, result =>
                {
                    tcs.SetResult(true);
                });
            });

            return tcs.Task;
        }

        #endregion

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

        #endregion

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
                    using (var dialog = new FolderBrowserDialog())
                    {
                        dialog.Description = title;

                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            return dialog.SelectedPath;
                        }
                    }

                    return null;
                });
            });
        }

        #endregion

        #region 同步方法（为了兼容旧代码）

        public bool ShowConfirmation(string message, string title = "确认")
        {
            return ShowConfirmationAsync(message, title).GetAwaiter().GetResult();
        }

        public void ShowInformation(string message, string title = "信息")
        {
            ShowInformationAsync(message, title).GetAwaiter().GetResult();
        }

        public void ShowWarning(string message, string title = "警告")
        {
            ShowWarningAsync(message, title).GetAwaiter().GetResult();
        }

        public void ShowError(string message, string title = "错误")
        {
            ShowErrorAsync(message, title).GetAwaiter().GetResult();
        }

        #endregion
    }
}