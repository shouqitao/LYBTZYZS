using System.Diagnostics;
using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels;

/// <summary>
/// API连接失败对话框ViewModel
/// enhance-shell-connection-dialog: 显示连接失败信息并提供恢复操作选项
/// 实现IDialogAware接口，符合Prism Dialog标准
/// </summary>
public class ApiConnectionFailedDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    #region 私有字段

    private string _title = "无法连接到服务器";
    private string _errorSummary = "无法连接到凌隐宝堂服务，请检查：";
    private List<string> _possibleReasons = [];
    private string _technicalDetails = string.Empty;
    private bool _isDetailsExpanded;
    private string _apiEndpoint = string.Empty;
    private string _errorType = string.Empty;

    #endregion

    #region 公共属性

    /// <summary>
    /// 对话框标题
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// 错误摘要信息
    /// </summary>
    public string ErrorSummary
    {
        get => _errorSummary;
        set => SetProperty(ref _errorSummary, value);
    }

    /// <summary>
    /// 可能原因列表
    /// </summary>
    public List<string> PossibleReasons
    {
        get => _possibleReasons;
        set => SetProperty(ref _possibleReasons, value);
    }

    /// <summary>
    /// 技术详情(可展开)
    /// </summary>
    public string TechnicalDetails
    {
        get => _technicalDetails;
        set => SetProperty(ref _technicalDetails, value);
    }

    /// <summary>
    /// 详情是否展开
    /// </summary>
    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    /// <summary>
    /// 离线模式是否可用 (v2.0启用)
    /// </summary>
    public bool IsOfflineModeEnabled { get; } = false;

    /// <summary>
    /// 离线模式提示文本
    /// </summary>
    public string OfflineModeTooltip { get; } = "离线模式将在v2.0版本中启用";

    #endregion

    #region 命令

    /// <summary>
    /// 重试命令
    /// </summary>
    public DelegateCommand RetryCommand { get; }

    /// <summary>
    /// 离线模式命令 (v2.0预留)
    /// </summary>
    public DelegateCommand OfflineModeCommand { get; }

    /// <summary>
    /// 查看日志命令
    /// </summary>
    public DelegateCommand ViewLogsCommand { get; }

    /// <summary>
    /// 退出命令
    /// </summary>
    public DelegateCommand ExitCommand { get; }

    #endregion

    #region IDialogAware 实现

    /// <summary>
    /// 对话框关闭请求事件
    /// </summary>
    public event Action<IDialogResult>? RequestClose;

    /// <summary>
    /// 是否可以关闭对话框
    /// </summary>
    public bool CanCloseDialog() => true;

    /// <summary>
    /// 对话框打开时调用
    /// </summary>
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 从参数中读取配置
        if (parameters.ContainsKey("ErrorMessage"))
        {
            var errorMessage = parameters.GetValue<string>("ErrorMessage");
            ErrorSummary = string.IsNullOrWhiteSpace(errorMessage)
                ? "无法连接到凌隐宝堂服务，请检查："
                : errorMessage;
        }

        if (parameters.ContainsKey("ApiEndpoint"))
        {
            _apiEndpoint = parameters.GetValue<string>("ApiEndpoint") ?? string.Empty;
        }

        if (parameters.ContainsKey("Exception"))
        {
            var exception = parameters.GetValue<Exception>("Exception");
            if (exception != null)
            {
                _errorType = exception.GetType().Name;
                BuildTechnicalDetails(exception);
            }
        }

        // 设置默认的可能原因
        PossibleReasons =
        [
            "WebAPI服务是否已启动",
            "网络连接是否正常",
            "防火墙是否阻止连接"
        ];

        Logger.LogInformation("ApiConnectionFailedDialog - 打开对话框，错误摘要：{ErrorSummary}", ErrorSummary);
    }

    /// <summary>
    /// 对话框关闭时调用
    /// </summary>
    public void OnDialogClosed()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 对话框已关闭");
    }

    #endregion

    #region 构造函数

    public ApiConnectionFailedDialogViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager)
        : base(eventAggregator, loggerFactory, regionManager, null, null)
    {
        RetryCommand = new DelegateCommand(ExecuteRetry);
        OfflineModeCommand = new DelegateCommand(ExecuteOfflineMode, CanExecuteOfflineMode);
        ViewLogsCommand = new DelegateCommand(ExecuteViewLogs);
        ExitCommand = new DelegateCommand(ExecuteExit);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 构建技术详情文本
    /// </summary>
    private void BuildTechnicalDetails(Exception exception)
    {
        var details = new System.Text.StringBuilder();
        details.AppendLine($"服务地址: {_apiEndpoint}");
        details.AppendLine($"错误类型: {exception.GetType().Name}");
        details.AppendLine($"详细信息: {exception.Message}");

        if (exception.InnerException != null)
        {
            details.AppendLine($"内部错误: {exception.InnerException.Message}");
        }

        TechnicalDetails = details.ToString();
    }

    /// <summary>
    /// 执行重试
    /// </summary>
    private void ExecuteRetry()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择重试");

        var result = new DialogResult(ButtonResult.Retry, new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.Retry }
        });

        RequestClose?.Invoke(result);
    }

    /// <summary>
    /// 执行离线模式 (v2.0预留)
    /// </summary>
    private void ExecuteOfflineMode()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择离线模式");

        var result = new DialogResult(ButtonResult.Yes, new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.OfflineMode }
        });

        RequestClose?.Invoke(result);
    }

    /// <summary>
    /// 检查是否可以执行离线模式
    /// </summary>
    private bool CanExecuteOfflineMode() => IsOfflineModeEnabled;

    /// <summary>
    /// 执行查看日志
    /// </summary>
    private void ExecuteViewLogs()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择查看日志");

        try
        {
            // 打开logs文件夹
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

            // 确保目录存在
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }

            // 使用文件资源管理器打开
            Process.Start(new ProcessStartInfo
            {
                FileName = logsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "打开日志文件夹失败");
        }

        // 对话框保持显示状态，不关闭
    }

    /// <summary>
    /// 执行退出
    /// </summary>
    private void ExecuteExit()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择退出");

        var result = new DialogResult(ButtonResult.Cancel, new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.Exit }
        });

        RequestClose?.Invoke(result);
    }

    #endregion
}
