using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Diagnostics;
using LYBT.WebAPI.Configuration.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 系统诊断控制器 - 提供运行时诊断和调试功能
/// refactor-logging-system: 新增控制器，支持运行时日志级别调整
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/diagnostics")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class DiagnosticsController : BaseApiController
{
    private readonly ISender _sender;

    public DiagnosticsController(
        ISender sender,
        ILogger<DiagnosticsController> logger)
        : base(logger)
    {
        _sender = sender;
    }

    /// <summary>
    /// 获取当前日志级别状态
    /// </summary>
    [HttpGet("logging/status")]
    public async Task<IActionResult> GetLoggingStatus()
    {
        var result = await _sender.Send(new GetLoggingStatusQuery());
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "获取日志状态失败");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 启用调试模式 - 临时降低日志级别以捕获更多诊断信息
    /// </summary>
    [HttpPost("logging/debug/enable")]
    public async Task<IActionResult> EnableDebugMode([FromBody] EnableDebugModeRequest? request)
    {
        var (operatorId, operatorName, _) = GetOperator();

        var result = await _sender.Send(new EnableDebugModeCommand(
            request?.Level, request?.DurationMinutes, operatorId, operatorName));

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "启用调试模式失败");
        return Success(result.Value!, "调试模式已启用");
    }

    /// <summary>
    /// 禁用调试模式 - 恢复默认日志级别
    /// </summary>
    [HttpPost("logging/debug/disable")]
    public async Task<IActionResult> DisableDebugMode()
    {
        var (operatorId, operatorName, _) = GetOperator();

        var result = await _sender.Send(new DisableDebugModeCommand(operatorId, operatorName));
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "禁用调试模式失败");
        return Success(result.Value!, "调试模式已禁用");
    }

    /// <summary>
    /// 设置指定的日志级别（无自动过期）
    /// </summary>
    [HttpPost("logging/level")]
    public async Task<IActionResult> SetLoggingLevel([FromBody] SetLoggingLevelRequest request)
    {
        var (operatorId, operatorName, _) = GetOperator();

        var result = await _sender.Send(new SetLoggingLevelCommand(
            request.Level, operatorId, operatorName));

        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "设置日志级别失败");
        return Success(result.Value!, "日志级别已更新");
    }
}


