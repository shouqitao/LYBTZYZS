using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/deploy")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
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
    public IActionResult Restart()
    {
        _logger.LogWarning("收到服务重启指令，2 秒后执行重启");
        _lifetime.StopApplication();
        return Success("服务重启指令已发送");
    }
}
