using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 桌面客户端下载主页（SHELL-010 决策 A 双端同步——本地模式为已安装环境，
/// 返回提示页；远程模式的发布包静态服务/下载页由 WebAPI 承担）
/// </summary>
[ApiController]
[Route("")]
public class DownloadController : BaseApiController
{
    public DownloadController(ILogger<DownloadController> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// GET / —— 本地模式提示页（公开）
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/")]
    [Produces("text/html")]
    public IActionResult Index()
    {
        var html = """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <title>凌隐宝堂中医诊所</title>
          <style>
            body { font-family: "Microsoft YaHei", sans-serif; background: #f5f7fa; margin: 0; display: flex; justify-content: center; align-items: center; min-height: 100vh; }
            .card { background: #fff; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,.08); padding: 40px 48px; text-align: center; }
            h1 { font-size: 20px; color: #1a2332; }
            p { color: #5f6b7a; font-size: 14px; }
          </style>
        </head>
        <body>
          <div class="card">
            <h1>凌隐宝堂中医诊所诊疗系统</h1>
            <p>本地模式运行中——桌面客户端已安装，无需下载。</p>
          </div>
        </body>
        </html>
        """;
        return Content(html, "text/html; charset=utf-8");
    }
}
