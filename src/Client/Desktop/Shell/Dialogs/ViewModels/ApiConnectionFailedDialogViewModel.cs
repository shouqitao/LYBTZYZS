using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels;

/// <summary>
/// API连接失败对话框ViewModel
/// enhance-shell-connection-dialog: 显示连接失败信息并提供恢复操作选项
/// OpenSpec: standardize-viewmodel-framework - 迁移到DialogViewModelBase
/// </summary>
public partial class ApiConnectionFailedDialogViewModel : DialogViewModelBase
{
    #region 私有字段

    private string _apiEndpoint = string.Empty;
    private string _errorType = string.Empty;

    #endregion

    #region 可观察属性

    /// <summary>
    /// 错误摘要信息
    /// </summary>
    [ObservableProperty]
    private string _errorSummary = "无法连接到凌隐宝堂服务，请检查：";

    /// <summary>
    /// 可能原因列表
    /// </summary>
    [ObservableProperty]
    private List<string> _possibleReasons = [];

    /// <summary>
    /// 技术详情(可展开)
    /// </summary>
    [ObservableProperty]
    private string _technicalDetails = string.Empty;

    /// <summary>
    /// 详情是否展开
    /// </summary>
    [ObservableProperty]
    private bool _isDetailsExpanded;

    #endregion

    #region 只读属性

    /// <summary>
    /// 离线模式是否可用 (v2.0启用)
    /// </summary>
    public bool IsOfflineModeEnabled { get; } = false;

    /// <summary>
    /// 离线模式提示文本
    /// </summary>
    public string OfflineModeTooltip { get; } = "离线模式将在v2.0版本中启用";

    #endregion

    #region 构造函数

    public ApiConnectionFailedDialogViewModel(
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
        : base(loggerFactory, eventAggregator)
    {
        Title = "无法连接到服务器";
    }

    #endregion

    #region 对话框生命周期

    /// <summary>
    /// 对话框打开时处理参数
    /// </summary>
    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        if (parameters == null) return;

        // 从参数中读取配置
        var errorMessage = GetDialogParameter(parameters, "ErrorMessage", string.Empty);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ErrorSummary = errorMessage;
        }

        _apiEndpoint = GetDialogParameter(parameters, "ApiEndpoint", string.Empty);

        if (parameters.TryGetValue<Exception>("Exception", out var exception) && exception != null)
        {
            _errorType = exception.GetType().Name;
            BuildTechnicalDetails(exception);
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
    /// 对话框关闭时清理
    /// </summary>
    protected override void OnDialogClosedCore()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 对话框已关闭");
    }

    #endregion

    #region 命令

    /// <summary>
    /// 重试命令
    /// </summary>
    [RelayCommand]
    private void Retry()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择重试");

        var result = new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.Retry }
        };

        CloseDialog(result, ButtonResult.Retry);
    }

    /// <summary>
    /// 离线模式命令 (v2.0预留)
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteOfflineMode))]
    private void OfflineMode()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择离线模式");

        var result = new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.OfflineMode }
        };

        CloseDialog(result, ButtonResult.Yes);
    }

    private bool CanExecuteOfflineMode() => IsOfflineModeEnabled;

    /// <summary>
    /// 查看日志命令
    /// </summary>
    [RelayCommand]
    private void ViewLogs()
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
    /// 退出命令
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        Logger.LogInformation("ApiConnectionFailedDialog - 用户选择退出");

        var result = new DialogParameters
        {
            { "RecoveryAction", RecoveryAction.Exit }
        };

        CloseDialog(result, ButtonResult.Cancel);
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

    #endregion
}
