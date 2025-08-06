using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 简单的对话框服务实现，不依赖 Prism 对话框
    /// </summary>
    public class SimpleDialogService : ICommonDialogService
    {
        // 异步方法
        public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task ShowErrorAsync(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
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

        public Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "")
        {
            // 简单实现，使用内置的输入框（需要引入 Microsoft.VisualBasic）
            // 或者返回 null 表示暂不支持
            return Task.FromResult<string?>(null);
        }

        public Task<string?> ShowOpenFileDialogAsync(string title = "打开文件", string filter = "所有文件|*.*")
        {
            var dialog = new OpenFileDialog
            {
                /* Title = title, */
                Filter = filter
            };
            
            if (dialog.ShowDialog() == true)
            {
                return Task.FromResult<string?>(dialog.FileName);
            }
            
            return Task.FromResult<string?>(null);
        }

        public Task<string?> ShowSaveFileDialogAsync(string title = "保存文件", string filter = "所有文件|*.*", string defaultFileName = "")
        {
            var dialog = new SaveFileDialog
            {
                /* Title = title, */
                Filter = filter,
                FileName = defaultFileName
            };
            
            if (dialog.ShowDialog() == true)
            {
                return Task.FromResult<string?>(dialog.FileName);
            }
            
            return Task.FromResult<string?>(null);
        }

        public Task<string?> ShowFolderBrowserDialogAsync(string title = "选择文件夹")
        {
            // WPF 没有内置的文件夹选择对话框，返回 null
            return Task.FromResult<string?>(null);
        }

        // 同步方法
        public bool ShowConfirmation(string message, string title = "确认")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public void ShowInformation(string message, string title = "信息")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}