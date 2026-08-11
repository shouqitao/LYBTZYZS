using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 重启操作确认请求体
/// </summary>
public record RestartConfirmDto(string? Confirm);

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/deploy")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)] // DEPLOY-PERM: 部署属运维操作——Admin 业务管理员无部署能力（US-SHELL-020 AC）
public class DeployController : BaseApiController
{
    private readonly IHostApplicationLifetime _lifetime;

    public DeployController(IHostApplicationLifetime lifetime, ILogger<DeployController> logger)
        : base(logger)
    {
        _lifetime = lifetime;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BusinessFail("未选择文件或文件为空");

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BusinessFail("仅支持 ZIP 格式的更新包");

        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"update_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("更新包已上传: {FileName}, 大小: {Size} bytes", fileName, file.Length);
        return Success(new { fileName, size = file.Length }, "更新包上传成功");
    }

    [HttpPost("restart")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult Restart([FromBody] RestartConfirmDto? request)
    {
        if (request?.Confirm != "RESTART")
            return ValidationFail("请确认重启操作：body 中 confirm 字段必须为 \"RESTART\"");

        _logger.LogWarning("收到服务重启指令，2 秒后执行重启");
        _lifetime.StopApplication();
        return Success("服务重启指令已发送");
    }
}
