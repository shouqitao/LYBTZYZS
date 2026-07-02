using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.WebAPI.Configuration.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/configuration")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class ConfigurationController : BaseApiController
{
    private readonly ISender _sender;

    public ConfigurationController(ISender sender, ILogger<ConfigurationController> logger)
        : base(logger)
    {
        _sender = sender;
    }

    /// <summary>
    /// 获取系统配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetConfigurationQuery(), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "获取配置失败");
        return Success(result.Value!, "查询成功");
    }

    /// <summary>
    /// 获取单个配置项
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetValue(string key, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetValueQuery(key), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(result.Error ?? "配置项不存在");
        return Success(result.Value, "查询成功");
    }

    /// <summary>
    /// 验证生产环境配置
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateProduction(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ValidateConfigurationQuery(), cancellationToken);
        if (!result.IsSuccess)
            return BusinessFail(result.Error ?? "配置验证失败");
        return Success("配置验证通过");
    }
}


