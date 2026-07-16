using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 部署管理API客户端接口 - 对应服务端 DeployController
/// </summary>
/// <remarks>
/// 功能范围: 上传更新包、重启服务
/// 权限要求: AdminOrSuperAdmin 策略
/// </remarks>
public interface IDeployApi
{
    /// <summary>
    /// 上传更新包
    /// </summary>
    [Refit.Post("/api/v1/deploy/upload")]
    Task<ApiResponse<object>> UploadAsync([Refit.Body] MultipartFormDataContent content);

    /// <summary>
    /// 重启服务
    /// </summary>
    [Refit.Post("/api/v1/deploy/restart")]
    Task<ApiResponse<object>> RestartAsync();
}
