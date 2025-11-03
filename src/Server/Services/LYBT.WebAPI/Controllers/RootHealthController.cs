using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 根路径健康检查控制器 - 用于基础健康检查和外部监控
/// 提供简单的 /health 端点，符合 Kubernetes/Docker 健康检查标准
/// </summary>
[ApiController]
[Route("health")]
public class RootHealthController : ControllerBase
{
    private static readonly DateTime _startupTime = DateTime.UtcNow;

    /// <summary>
    /// 基础健康检查端点
    /// GET /health
    /// </summary>
    /// <returns>简单的健康状态响应</returns>
    [HttpGet]
    [AllowAnonymous]  // 允许匿名访问，便于外部监控工具
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            uptime = (long)(DateTime.UtcNow - _startupTime).TotalSeconds
        });
    }

    /// <summary>
    /// Ping端点 - 最轻量级的存活性检查
    /// GET /health/ping
    /// </summary>
    /// <returns>Pong响应</returns>
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }
}
