using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Views.Dialogs
{
    /// <summary>
    /// UltraThink Phase 5.3: 严重错误对话框
    /// 显示详细的错误信息和恢复建议
    /// </summary>
    public partial class CriticalErrorDialog : Window
    {
        private HandledError? _errorInfo;

        public HandledError? ErrorInfo
        {
            get => _errorInfo;
            set
            {
                _errorInfo = value;
                PopulateErrorInfo();
            }
        }

        public CriticalErrorDialog()
        {
            InitializeComponent();
            
            // 设置窗口属性
            this.WindowStyle = WindowStyle.ToolWindow;
            this.ShowActivated = true;
            this.Topmost = true;
        }

        private void PopulateErrorInfo()
        {
            if (_errorInfo == null)
                return;

            try
            {
                // 用户消息
                UserMessageText.Text = _errorInfo.UserMessage ?? "发生了未知错误";

                // 建议操作
                if (_errorInfo.SuggestedActions?.Count > 0)
                {
                    SuggestedActionsPanel.ItemsSource = _errorInfo.SuggestedActions;
                }
                else
                {
                    SuggestedActionsPanel.ItemsSource = new[] { "请联系技术支持" };
                }

                // 技术信息
                ErrorTypeText.Text = _errorInfo.OriginalException?.GetType().Name ?? "Unknown";
                TimestampText.Text = _errorInfo.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                ErrorIdText.Text = _errorInfo.Id.ToString();

                // 技术详情
                TechnicalDetailsText.Text = _errorInfo.TechnicalDetails ?? 
                    _errorInfo.OriginalException?.Message ?? "无技术详情";

                // 堆栈跟踪
                StackTraceText.Text = _errorInfo.OriginalException?.StackTrace ?? "无堆栈信息";

                // 根据错误严重程度调整窗口标题
                this.Title = _errorInfo.Severity switch
                {
                    ErrorSeverity.Fatal => "致命错误",
                    ErrorSeverity.Critical => "严重错误",
                    _ => "错误"
                };
            }
            catch (Exception ex)
            {
                // 防止显示错误信息时再次出错
                UserMessageText.Text = "显示错误信息时发生异常：" + ex.Message;
                TechnicalDetailsText.Text = ex.ToString();
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var errorReport = GenerateErrorReport();
                Clipboard.SetText(errorReport);
                
                // 显示复制成功的视觉反馈
                var originalContent = CopyButton.Content;
                CopyButton.Content = "已复制!";
                CopyButton.IsEnabled = false;
                
                // 2秒后恢复按钮状态
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    CopyButton.Content = originalContent;
                    CopyButton.IsEnabled = true;
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 生成错误报告并尝试打开邮件客户端
                var errorReport = GenerateErrorReport();
                var subject = Uri.EscapeDataString($"错误报告 - {_errorInfo?.OriginalException?.GetType().Name}");
                var body = Uri.EscapeDataString(errorReport);
                
                var mailto = $"mailto:support@lybt.com?subject={subject}&body={body}";
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // 如果无法打开邮件客户端，复制错误信息到剪贴板
                MessageBox.Show($"无法打开邮件客户端：{ex.Message}\n\n错误信息已复制到剪贴板，请手动发送给技术支持。", 
                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                CopyButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private string GenerateErrorReport()
        {
            if (_errorInfo == null)
                return "无错误信息";

            var report = $@"=== 凌隐宝堂系统错误报告 ===

错误时间: {_errorInfo.Timestamp:yyyy-MM-dd HH:mm:ss}
错误ID: {_errorInfo.Id}
错误类别: {_errorInfo.Category}
错误严重程度: {_errorInfo.Severity}

用户消息:
{_errorInfo.UserMessage}

建议操作:
{string.Join("\n", _errorInfo.SuggestedActions?.Select(a => $"• {a}") ?? new[] { "• 联系技术支持" })}

技术详情:
{_errorInfo.TechnicalDetails ?? _errorInfo.OriginalException?.Message ?? "无"}

错误类型:
{_errorInfo.OriginalException?.GetType().FullName ?? "Unknown"}

堆栈跟踪:
{_errorInfo.OriginalException?.StackTrace ?? "无"}

系统信息:
- 操作系统: {Environment.OSVersion}
- .NET版本: {Environment.Version}
- 工作目录: {Environment.CurrentDirectory}
- 用户: {Environment.UserName}
- 机器名: {Environment.MachineName}

=== 报告结束 ===";

            return report;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 确保窗口显示在最前面
            this.Activate();
            this.Focus();
        }
    }
}