using Asp.Versioning;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Constants;
using System.Threading;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize(Policy = PolicyConstants.SuperAdminOnly)]
public class ConfigurationController : BaseApiController
{
    private readonly ISystemConfigurationService _configurationService;

    public ConfigurationController(ISystemConfigurationService configurationService, ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// 获取系统配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var result = await _configurationService.GetConfigurationAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetValue(string key, CancellationToken cancellationToken)
    {
        var result = await _configurationService.GetValueAsync(key, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateProduction(CancellationToken cancellationToken)
    {
        var result = await _configurationService.ValidateProductionConfigAsync(cancellationToken);
        return HandleResult(result);
    }
}
