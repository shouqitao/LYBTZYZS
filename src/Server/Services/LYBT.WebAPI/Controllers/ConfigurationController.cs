using Asp.Versioning;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 系统配置 API - 配置读写、生产环境验证
/// 权限隔离（2026-08-13 修复）：配置管理 = sysadmin 专属——业务管理员（Admin）不应访问系统配置
/// （US-SHELL-018「角色: sysadmin」；类级统一 SysAdminOnly，覆盖全部端点）
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize(Policy = PolicyConstants.SysAdminOnly)]
public class ConfigurationController : BaseApiController
{
    private readonly ISystemConfigurationService _configurationService;
    private readonly ISecurityAuditService _auditService;

    // SHELL-018 Phase 1: 重启限频（每小时 ≤3 次）
    private static readonly System.Collections.Concurrent.ConcurrentQueue<DateTime> RestartRequests = new();
    private static readonly TimeSpan RestartWindow = TimeSpan.FromHours(1);
    private const int RestartMaxPerHour = 3;

    public ConfigurationController(
        ISystemConfigurationService configurationService,
        ISecurityAuditService auditService,
        ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _configurationService = configurationService;
        _auditService = auditService;
    }

    /// <summary>
    /// 获取系统配置
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var result = await _configurationService.GetConfigurationAsync(cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "获取配置失败");
        return Success(result.Data!, "查询成功");
    }

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetValue(string key, CancellationToken cancellationToken)
    {
        var result = await _configurationService.GetValueAsync(key, cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage ?? "配置项不存在");
        return Success(result.Data, "查询成功");
    }

    /// <summary>
    /// 修改单个配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetValue(string key, [FromBody] string value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ValidationFail("配置项名称不能为空");

        var result = await _configurationService.SetValueAsync(key, value, cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "修改配置失败");
        return Success("配置修改成功");
    }

    /// <summary>
    /// 批量修改配置项（白名单校验 + 持久化 + 热更新）
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfiguration([FromBody] Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        if (settings is null || settings.Count == 0)
            return ValidationFail("配置项集合不能为空");

        var result = await _configurationService.UpdateConfigurationAsync(settings, cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "批量修改配置失败");
        return Success($"批量修改 {settings.Count} 项配置成功");
    }

    /// <summary>
    /// 获取单节配置（SHELL-018 Phase 1: 敏感键掩码脱敏；sysadmin 专属）
    /// </summary>
    [HttpGet("sections/{section}")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSection(string section, CancellationToken ct)
    {
        var result = await _configurationService.GetSectionAsync(section, ct);
        if (!result.IsSuccess)
            return NotFound(result.ErrorMessage ?? "配置节不存在");
        await RecordAuditAsync("ConfigGet", $"{section}（{result.Data!.Count} 键）");
        return Success(result.Data, "查询成功");
    }

    /// <summary>
    /// 修改单节配置（SHELL-018 Phase 1: 白名单逐键 + 持久化 + 生效语义；sysadmin 专属）
    /// </summary>
    [HttpPut("sections/{section}")]
    [ProducesResponseType(typeof(ApiResponse<ConfigUpdateResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSection(string section, [FromBody] Dictionary<string, string> values, CancellationToken ct)
    {
        if (values is null || values.Count == 0)
            return ValidationFail("配置项集合不能为空");

        var result = await _configurationService.UpdateSectionAsync(section, values, ct);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "修改配置失败");

        await RecordAuditAsync("ConfigUpdate", $"{section}（{result.Data!.UpdatedCount} 键，{result.Data.EffectiveMode} 生效）");
        return Success(result.Data, "配置修改成功");
    }

    /// <summary>
    /// 延迟重启（SHELL-018 Phase 1: sysadmin 专属 + 限频每小时 3 次 + 30 秒延迟）
    /// </summary>
    [HttpPost("restart")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Restart([FromServices] IHostApplicationLifetime lifetime, CancellationToken ct)
    {
        if (!TryAcquireRestartSlot())
            return BusinessFail("重启请求过于频繁（每小时最多 3 次），请稍后再试");

        await RecordAuditAsync("ConfigRestart", "延迟重启已调度（30 秒后停止）");
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            lifetime.StopApplication();
        }, CancellationToken.None);
        return Success("重启已调度，将在 30 秒后生效");
    }

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateProduction(CancellationToken cancellationToken)
    {
        var result = await _configurationService.ValidateProductionConfigAsync(cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.ErrorMessage ?? "配置验证失败");
        return Success("配置验证通过");
    }

    /// <summary>
    /// 记录配置操作审计（SHELL-018 Phase 1: 每次 GET/PUT/Restart）
    /// </summary>
    private async Task RecordAuditAsync(string eventType, string details)
    {
        try
        {
            var (operatorId, operatorName, _) = GetOperator();
            await _auditService.RecordEventAsync(new SecurityAuditEvent
            {
                UserId = operatorId,
                UserName = operatorName,
                EventType = eventType,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                Details = details,
                IsSuccess = true
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CFG] 配置审计记录失败: {EventType}", eventType);
        }
    }

    /// <summary>
    /// 重启限频（滑动窗口每小时 ≤3）
    /// </summary>
    private static bool TryAcquireRestartSlot()
    {
        var now = DateTime.UtcNow;
        while (RestartRequests.TryPeek(out var oldest) && now - oldest > RestartWindow)
            RestartRequests.TryDequeue(out _);

        if (RestartRequests.Count >= RestartMaxPerHour)
            return false;

        RestartRequests.Enqueue(now);
        return true;
    }
}
