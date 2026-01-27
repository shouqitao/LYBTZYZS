using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Views;
using LYBT.Desktop.Infrastructure.ViewModels;
using Microsoft.Win32;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 通用对话框服务实现 - Epic #1934
    /// MVP阶段基于WPF原生对话框的简单实现
    /// OpenSpec: unify-dialog-to-prism - 统一使用Prism DialogService
    /// </summary>
    public class CommonDialogService : ICommonDialogService
    {
        private readonly IDialogService _dialogService;

        public CommonDialogService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        public Task ShowInfoAsync(string message, string? title = null)
        {
            System.Windows.MessageBox.Show(message, title ?? "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public Task ShowWarningAsync(string message, string? title = null)
        {
            System.Windows.MessageBox.Show(message, title ?? "警告", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public Task ShowErrorAsync(string message, string? title = null)
        {
            System.Windows.MessageBox.Show(message, title ?? "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            var result = System.Windows.MessageBox.Show(message, title ?? "确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            return Task.FromResult(result == System.Windows.MessageBoxResult.Yes);
        }

        /// <summary>
        /// 显示三选项对话框（是/否/取消）
        /// Issue #2247: 支持离开确认等三选项场景
        /// </summary>
        public Task<TripleChoiceResult> ShowTripleChoiceAsync(string message, string? title = null)
        {
            var result = System.Windows.MessageBox.Show(message, title ?? "确认", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
            var choice = result switch
            {
                System.Windows.MessageBoxResult.Yes => TripleChoiceResult.Yes,
                System.Windows.MessageBoxResult.No => TripleChoiceResult.No,
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
        /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
        /// OpenSpec: unify-dialog-to-prism - 迁移到Prism DialogService
        /// </summary>
        public Task<UnfinishedCaseChoice> ShowUnfinishedCaseDialogAsync(string patientName)
        {
            var tcs = new TaskCompletionSource<UnfinishedCaseChoice>();

            var parameters = new DialogParameters
            {
                { "PatientName", patientName }
            };

            _dialogService.ShowDialog(
                nameof(UnfinishedCaseDialog),
                parameters,
                result =>
                {
                    if (result.Parameters.TryGetValue<UnfinishedCaseChoice>("Result", out var choice))
                    {
                        tcs.SetResult(choice);
                    }
                    else
                    {
                        tcs.SetResult(UnfinishedCaseChoice.Cancel);
                    }
                });

            return tcs.Task;
        }
    }
}
