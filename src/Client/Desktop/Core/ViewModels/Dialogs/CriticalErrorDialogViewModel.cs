using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.ViewModels.Dialogs
{
    /// <summary>
    /// 严重错误对话框视图模型
    /// UltraThink Command绑定优化：消除Click事件处理器，使用Command绑定
    /// </summary>
    public class CriticalErrorDialogViewModel : DialogViewModel
    {
        private HandledError? _errorInfo;
        private bool _isCopyEnabled = true;
        private string _copyButtonText = "复制错误信息";

        #region Properties

        /// <summary>
        /// 错误信息
        /// </summary>
        public HandledError? ErrorInfo
        {
            get => _errorInfo;
            set
            {
                if (SetProperty(ref _errorInfo, value))
                {
                    PopulateErrorInfo();
                }
            }
        }

        /// <summary>
        /// 复制按钮是否可用
        /// </summary>
        public bool IsCopyEnabled
        {
            get => _isCopyEnabled;
            set => SetProperty(ref _isCopyEnabled, value);
        }

        /// <summary>
        /// 复制按钮文本
        /// </summary>
        public string CopyButtonText
        {
            get => _copyButtonText;
            set => SetProperty(ref _copyButtonText, value);
        }

        /// <summary>
        /// 用户消息
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// 建议操作列表
        /// </summary>
        public string[] SuggestedActions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 错误类型
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>
        /// 错误ID
        /// </summary>
        public string ErrorId { get; set; } = string.Empty;

        /// <summary>
        /// 技术详情
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;

        /// <summary>
        /// 堆栈跟踪
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string WindowTitle { get; set; } = SystemConstants.ErrorTitle;

        #endregion

        #region Commands

        /// <summary>
        /// 复制错误信息命令
        /// </summary>
        public DelegateCommand CopyCommand { get; } = null!;

        /// <summary>
        /// 报告问题命令
        /// </summary>
        public DelegateCommand ReportCommand { get; } = null!;

        /// <summary>
        /// 关闭命令
        /// </summary>
        public DelegateCommand CloseCommand { get; } = null!;

        #endregion

        #region Events

        /// <summary>
        /// 请求关闭事件
        /// </summary>
        public event Action<bool?> RequestClose = delegate { };

        #endregion

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        public CriticalErrorDialogViewModel(
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            DialogTitle = SystemConstants.ErrorTitle;

            // 初始化命令
            CopyCommand = new DelegateCommand(async () => await ExecuteCopyAsync(), () => IsCopyEnabled);
            ReportCommand = new DelegateCommand(async () => await ExecuteReportAsync());
            CloseCommand = new DelegateCommand(ExecuteClose);

            // 监听属性变化
            CopyCommand.ObservesProperty(() => IsCopyEnabled);
        }

        #endregion

        #region DialogViewModel Implementation

        protected override Task<bool> SaveAsync()
        {
            // 对于错误对话框，没有保存操作
            return Task.FromResult(true);
        }

        protected override bool CanSave()
        {
            return false; // 错误对话框不需要保存按钮
        }

        protected override void InitializeDialog()
        {
            base.InitializeDialog();
            // 隐藏保存按钮，错误对话框不需要
            SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// 执行复制命令
        /// </summary>
        private async Task ExecuteCopyAsync()
        {
            try
            {
                var errorReport = GenerateErrorReport();
                Clipboard.SetText(errorReport);

                // 显示复制成功的视觉反馈
                var originalText = CopyButtonText;
                CopyButtonText = "已复制!";
                IsCopyEnabled = false;

                // 2秒后恢复按钮状态
                await Task.Delay(2000);
                
                CopyButtonText = originalText;
                IsCopyEnabled = true;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("复制错误信息", ex);
            }
        }

        /// <summary>
        /// 执行报告命令
        /// </summary>
        private async Task ExecuteReportAsync()
        {
            try
            {
                // 生成错误报告并尝试打开邮件客户端
                var errorReport = GenerateErrorReport();
                var subject = Uri.EscapeDataString($"错误报告 - {_errorInfo?.Exception?.GetType().Name}");
                var body = Uri.EscapeDataString(errorReport);

                var mailto = $"mailto:support@lybt.com?subject={subject}&body={body}";

                // 添加await以修复CS1998警告
                await Task.Run(() => Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                }));
            }
            catch (Exception ex)
            {
                // 如果无法打开邮件客户端，复制错误信息到剪贴板
                ErrorMessage = $"无法打开邮件客户端：{ex.Message}\n\n错误信息已复制到剪贴板，请手动发送给技术支持。";
                await ExecuteCopyAsync();
            }
        }

        /// <summary>
        /// 执行关闭命令
        /// </summary>
        private void ExecuteClose()
        {
            RequestClose(true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 填充错误信息
        /// </summary>
        private void PopulateErrorInfo()
        {
            if (_errorInfo == null)
                return;

            try
            {
                // 用户消息
                UserMessage = _errorInfo.UserMessage ?? "发生了未知错误";

                // 建议操作
                if (_errorInfo.SuggestedActions?.Count > 0)
                {
                    SuggestedActions = _errorInfo.SuggestedActions.ToArray();
                }
                else
                {
                    SuggestedActions = new[] { "请联系技术支持" };
                }

                // 技术信息
                ErrorType = _errorInfo.Exception?.GetType().Name ?? "Unknown";
                Timestamp = _errorInfo.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss");
                ErrorId = _errorInfo.Id.ToString();

                // 技术详情
                TechnicalDetails = _errorInfo.TechnicalDetails ??
                    _errorInfo.Exception?.Message ?? "无技术详情";

                // 堆栈跟踪
                StackTrace = _errorInfo.Exception?.StackTrace ?? "无堆栈信息";

                // 根据错误严重程度调整窗口标题
                WindowTitle = _errorInfo.Severity switch
                {
                    ErrorSeverity.Fatal => "致命错误",
                    ErrorSeverity.Critical => "严重错误",
                    _ => "错误"
                };

                // 通知属性变更
                RaisePropertyChanged(nameof(UserMessage));
                RaisePropertyChanged(nameof(SuggestedActions));
                RaisePropertyChanged(nameof(ErrorType));
                RaisePropertyChanged(nameof(Timestamp));
                RaisePropertyChanged(nameof(ErrorId));
                RaisePropertyChanged(nameof(TechnicalDetails));
                RaisePropertyChanged(nameof(StackTrace));
                RaisePropertyChanged(nameof(WindowTitle));
            }
            catch (Exception ex)
            {
                // 防止显示错误信息时再次出错
                UserMessage = "显示错误信息时发生异常：" + ex.Message;
                TechnicalDetails = ex.ToString();
                RaisePropertyChanged(nameof(UserMessage));
                RaisePropertyChanged(nameof(TechnicalDetails));
            }
        }

        /// <summary>
        /// 生成错误报告
        /// </summary>
        private string GenerateErrorReport()
        {
            if (_errorInfo == null)
                return "无错误信息";

            var report = $@"=== 凌隐宝堂系统错误报告 ===

错误时间: {_errorInfo.OccurredAt:yyyy-MM-dd HH:mm:ss}
错误ID: {_errorInfo.Id}
错误类别: {_errorInfo.Category}
错误严重程度: {_errorInfo.Severity}

用户消息:
{_errorInfo.UserMessage}

建议操作:
{string.Join("\n", _errorInfo.SuggestedActions?.Select(a => $"• {a}") ?? new[] { "• 联系技术支持" })}

技术详情:
{_errorInfo.TechnicalDetails ?? _errorInfo.Exception?.Message ?? "无"}

错误类型:
{_errorInfo.Exception?.GetType().FullName ?? "Unknown"}

堆栈跟踪:
{_errorInfo.Exception?.StackTrace ?? "无"}

系统信息:
- 操作系统: {Environment.OSVersion}
- .NET版本: {Environment.Version}
- 工作目录: {Environment.CurrentDirectory}
- 用户: {Environment.UserName}
- 机器名: {Environment.MachineName}

=== 报告结束 ===";

            return report;
        }

        #endregion
    }
}