using System.Security.Cryptography;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 部署服务实现（P1-4 2026-08-14: 更新包上传从 DeployController 移入——Controller 仅编排）
/// </summary>
public class DeployService : IDeployService
{
    private readonly ILogger<DeployService> _logger;

    public DeployService(ILogger<DeployService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<(string FileName, long Size)>> SaveUpdatePackageAsync(
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        if (content == null || content.Length == 0)
            return Result<(string, long)>.Failure(LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode.InvalidRequest, "未选择文件或文件为空");

        if (!originalFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return Result<(string, long)>.Failure(LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode.InvalidRequest, "仅支持 ZIP 格式的更新包");

        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"update_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(stream, cancellationToken);

        // P0-2: 计算 SHA256 并写入 .update-pending + .update-pending.sha256（供 Program 热更新校验）
        stream.Close(); // 确保落盘后再计算
        var sha256 = await ComputeSha256Async(filePath, cancellationToken);
        var flagPath = Path.Combine(AppContext.BaseDirectory, ".update-pending");
        var shaPath = Path.Combine(AppContext.BaseDirectory, ".update-pending.sha256");
        await File.WriteAllTextAsync(flagPath, filePath, cancellationToken);
        await File.WriteAllTextAsync(shaPath, sha256, cancellationToken);

        _logger.LogInformation("更新包已上传: {FileName}, 大小: {Size} bytes, SHA256: {Sha256}", fileName, content.Length, sha256);
        return Result<(string, long)>.Success((fileName, content.Length));
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
