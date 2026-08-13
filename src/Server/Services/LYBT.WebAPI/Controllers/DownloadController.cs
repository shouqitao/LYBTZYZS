using LYBT.Infrastructure.Web;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 桌面客户端下载主页（SHELL-010 决策 A: GET / 返回极简 HTML 下载页——公开可访问；
/// 发布包静态服务由 DesktopUpdate:ReleasesPath 配置驱动——UseStaticFiles 托管 /releases/）
/// </summary>
[ApiController]
[Route("")]
[Authorize] // P09: 类级授权（公开端点经方法级 AllowAnonymous 豁免——下载页 US-SHELL-010 决策 A）
public class DownloadController : BaseApiController
{
    private readonly IDownloadService _downloadService;

    public DownloadController(
        IDownloadService downloadService,
        ILogger<DownloadController> logger)
        : base(logger)
    {
        _downloadService = downloadService;
    }

    /// <summary>
    /// 下载主页：GET / —— 显示桌面客户端下载 + 版本号/更新时间（公开，无需认证）
    /// P1-6（2026-08-14）: 文件扫描 + HTML 生成移入 IDownloadService——Controller 仅编排
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/")]
    [Produces("text/html")]
    public IActionResult Index()
    {
        var html = _downloadService.BuildDownloadPageHtml();
        return Content(html, "text/html; charset=utf-8");
    }
}
