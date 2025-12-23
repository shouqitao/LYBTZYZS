namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// API连接恢复服务接口
/// enhance-shell-connection-dialog: 负责处理API连接失败后的用户交互和恢复流程
/// </summary>
public interface IApiConnectionRecoveryService
{
    /// <summary>
    /// 显示连接失败对话框并获取用户选择的恢复操作
    /// </summary>
    /// <param name="errorMessage">错误摘要信息</param>
    /// <param name="exception">原始异常(可选)</param>
    /// <param name="apiEndpoint">API端点地址(可选)</param>
    /// <returns>用户选择的恢复操作</returns>
    Task<RecoveryAction> ShowConnectionFailedDialogAsync(
        string errorMessage,
        Exception? exception = null,
        string? apiEndpoint = null);
}
