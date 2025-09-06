using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels {

    /// <summary>
    /// 错误详情对话框视图模型
    /// </summary>
    public class ErrorDetailsDialogViewModel : BindableBase {
        private SharedCommon.HandledError _handledError;

        public SharedCommon.HandledError HandledError {
            get => _handledError;
            set {
                SetProperty(ref _handledError, value);
                UpdateProperties();
            }
        }

        // 显示属性
        public string Id => HandledError?.Id ?? string.Empty;

        public string UserMessage => HandledError?.UserMessage ?? string.Empty;
        public SharedCommon.ErrorCategory Category => HandledError?.Category ?? SharedCommon.ErrorCategory.Unknown;
        public SharedCommon.ErrorSeverity Severity => HandledError?.Severity ?? SharedCommon.ErrorSeverity.Error;
        public DateTime Timestamp => HandledError?.OccurredAt ?? DateTime.Now;
        public string TechnicalDetails => HandledError?.TechnicalDetails ?? string.Empty;
        public bool CanRetry => HandledError?.CanRetry ?? false;
        public List<string> SuggestedActions => HandledError?.SuggestedActions ?? new List<string>();
        public bool HasSuggestedActions => SuggestedActions.Any();

        // 上下文信息
        public string Module => HandledError?.Module ?? string.Empty;

        public List<KeyValuePair<string, string>> ContextData => GetContextData();

        // 命令
        public DelegateCommand CloseCommand { get; }

        public DelegateCommand RetryCommand { get; }
        public DelegateCommand CopyErrorCommand { get; }

        // 事件
        public event EventHandler? CloseRequested;

        public event EventHandler? RetryRequested;

        public ErrorDetailsDialogViewModel(SharedCommon.HandledError handledError) {
            _handledError = handledError ?? throw new ArgumentNullException(nameof(handledError));

            CloseCommand = new DelegateCommand(ExecuteClose);
            RetryCommand = new DelegateCommand(ExecuteRetry, CanExecuteRetry);
            CopyErrorCommand = new DelegateCommand(ExecuteCopyError);
        }

        private void UpdateProperties() {
            RaisePropertyChanged(nameof(Id));
            RaisePropertyChanged(nameof(UserMessage));
            RaisePropertyChanged(nameof(Category));
            RaisePropertyChanged(nameof(Severity));
            RaisePropertyChanged(nameof(Timestamp));
            RaisePropertyChanged(nameof(TechnicalDetails));
            RaisePropertyChanged(nameof(CanRetry));
            RaisePropertyChanged(nameof(SuggestedActions));
            RaisePropertyChanged(nameof(HasSuggestedActions));
            RaisePropertyChanged(nameof(ContextData));

            RetryCommand.RaiseCanExecuteChanged();
        }

        private List<KeyValuePair<string, string>> GetContextData() {
            var data = new List<KeyValuePair<string, string>>();

            // 简化上下文数据显示
            if (HandledError != null) {
                data.Add(new KeyValuePair<string, string>("错误ID", Id));
                data.Add(new KeyValuePair<string, string>("模块", Module));
                data.Add(new KeyValuePair<string, string>("发生时间", Timestamp.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            return data;
        }

        private void ExecuteClose() {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ExecuteRetry() {
            RetryRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanExecuteRetry() {
            return CanRetry;
        }

        private void ExecuteCopyError() {
            try {
                var errorInfo = BuildErrorSummary();
                Clipboard.SetText(errorInfo);

                // 可以显示一个简短的成功提示
                // 这里暂时使用调试输出
                System.Diagnostics.Debug.WriteLine("错误信息已复制到剪贴板");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"复制错误信息失败: {ex.Message}");
            }
        }

        private string BuildErrorSummary() {
            var summary = $@"错误详情报告
=====================================

错误ID: {Id}
用户消息: {UserMessage}
错误类型: {Category}
严重程度: {Severity}
发生时间: {Timestamp:yyyy-MM-dd HH:mm:ss}
是否可重试: {(CanRetry ? "是" : "否")}

操作上下文:
- 模块名称: {Module}
- 错误类型: {Category}
- 严重程度: {Severity}

建议操作:
{(HasSuggestedActions ? string.Join("\n", SuggestedActions.Select(a => $"• {a}")) : "无")}

技术详情:
{TechnicalDetails}

附加数据:
{(ContextData.Any() ? string.Join("\n", ContextData.Select(kvp => $"• {kvp.Key}: {kvp.Value}")) : "无")}

=====================================
生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            return summary;
        }
    }
}
