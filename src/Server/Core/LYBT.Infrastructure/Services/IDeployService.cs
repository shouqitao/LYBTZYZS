using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 部署服务（P1-4 2026-08-14: 更新包上传逻辑移出 DeployController——Controller 仅编排）
/// </summary>
public interface IDeployService
{
    /// <summary>
    /// 保存更新包到 uploads 目录（扩展名校验 + 目录创建 + 文件写入）
    /// </summary>
    /// <returns>成功时 Data = (fileName, size)；失败时 ErrorMessage</returns>
    Task<Result<(string FileName, long Size)>> SaveUpdatePackageAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);
}
