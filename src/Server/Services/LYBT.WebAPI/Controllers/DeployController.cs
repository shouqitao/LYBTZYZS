using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Services;
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
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)] // DEPLOY-PERM: 部署属运维操作——Admin 业务管理员无部署能力（US-SHELL-020 AC）
public class DeployController : BaseApiController
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IDeployService _deployService;

    public DeployController(
        IHostApplicationLifetime lifetime,
        IDeployService deployService,
        ILogger<DeployController> logger)
        : base(logger)
    {
        _lifetime = lifetime;
        _deployService = deployService;
    }

    /// <summary>
    /// 上传桌面客户端更新包（US-SHELL-020——发布包落盘 ReleasesPath，供自动升级分发）
    /// </summary>
    /// <param name="file">更新包文件（Setup-*.exe，扩展名白名单校验）</param>
    /// <param name="ct">取消令牌</param>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        // P1-4（2026-08-14）: 上传逻辑移入 IDeployService——Controller 仅编排
        await using var stream = file.OpenReadStream();
        var result = await _deployService.SaveUpdatePackageAsync(stream, file.FileName, ct);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "上传失败");

        return Success(
            new { fileName = result.Data!.FileName, size = result.Data.Size },
            "更新包上传成功");
    }

    /// <summary>
    /// 重启 WebAPI 服务（需 body 显式确认——confirm 字段必须为 "RESTART"）
    /// </summary>
    /// <param name="request">重启确认请求体（confirm=RESTART）</param>
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
