using System.Windows;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Win32;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 通用对话框服务实现 - Epic #1934
    /// MVP阶段基于WPF原生对话框的简单实现
    /// </summary>
    public class CommonDialogService : ICommonDialogService
    {
        /// <summary>
        /// 显示信息消息
        /// </summary>
        public Task ShowInfoAsync(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public Task ShowWarningAsync(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public Task ShowErrorAsync(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var result = MessageBox.Show(message, title ?? "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        /// <summary>
        /// 显示三选项对话框（是/否/取消）
        /// Issue #2247: 支持离开确认等三选项场景
        /// </summary>
        public Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null)
        {
            var result = MessageBox.Show(message, title ?? "确认", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            var choice = result switch
            {
                MessageBoxResult.Yes => TripleChoiceResult.Yes,
                MessageBoxResult.No => TripleChoiceResult.No,
                _ => TripleChoiceResult.Cancel
            };
            return Task.FromResult(choice);
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <remarks>
        /// MVP阶段暂不实现，返回null
        /// </remarks>
        public Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null)
        {
            // MVP阶段暂不实现自定义输入对话框
            return Task.FromResult<string?>(null);
        }

        /// <summary>
        /// 显示文件选择对话框 (Epic #1934 FR-001)
        /// </summary>
        public Task<string?> ShowOpenFileDialogAsync(string? filter = null, string? title = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter ?? "所有文件|*.*",
                Title = title ?? "选择文件"
            };

            var result = dialog.ShowDialog();
            return Task.FromResult(result == true ? dialog.FileName : null);
        }

        /// <summary>
        /// 显示文件保存对话框 (Epic #1934 FR-002, FR-003)
        /// </summary>
        public Task<string?> ShowSaveFileDialogAsync(string? filter = null, string? title = null, string? defaultFileName = null)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter ?? "所有文件|*.*",
                Title = title ?? "保存文件",
                FileName = defaultFileName ?? string.Empty
            };

            var result = dialog.ShowDialog();
            return Task.FromResult(result == true ? dialog.FileName : null);
        }

        /// <summary>
        /// 显示未完成医案四选项对话框
        /// OpenSpec: optimize-medicalcase-navigation
        /// 使用自定义UnfinishedCaseDialog实现四选项交互
        /// </summary>
        public Task<UnfinishedCaseChoice> ShowUnfinishedCaseDialogAsync(string patientName)
        {
            var dialog = new Views.UnfinishedCaseDialog();
            dialog.SetPatientName(patientName);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return Task.FromResult(dialog.Result);
        }
    }
}
