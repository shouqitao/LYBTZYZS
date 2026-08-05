using Asp.Versioning;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 系统配置 API - 配置读写、生产环境验证
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class ConfigurationController : BaseApiController
{
    private readonly ISystemConfigurationService _configurationService;

    public ConfigurationController(
        ISystemConfigurationService configurationService,
        ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _configurationService = configurationService;
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
}
