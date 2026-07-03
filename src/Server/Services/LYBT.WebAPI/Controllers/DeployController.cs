using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 部署管理 API - 支持在线热更新
/// </summary>
[ApiController]
[Route("api/deploy")]
[Authorize(Policy = "AdminOrSuperAdmin")]
public class DeployController : BaseApiController
{
    private readonly IWebHostEnvironment _env;
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "lybt-deploy");

    public DeployController(
        IWebHostEnvironment env,
        ILogger<DeployController> logger) : base(logger)
    {
        _env = env;
    }

    /// <summary>
    /// 上传更新包
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ValidationFail("请上传更新包文件");

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return ValidationFail("仅支持 .zip 格式");

        if (!Directory.Exists(TempDir))
            Directory.CreateDirectory(TempDir);

        var zipPath = Path.Combine(TempDir, "update.zip");
        using (var stream = new FileStream(zipPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        LogOperation("上传更新包", new { FileName = file.FileName, Size = file.Length });
        return Success($"更新包已上传 ({file.Length / 1024} KB)，请调用 /api/deploy/restart 执行更新");
    }

    /// <summary>
    /// 执行更新重启
    /// </summary>
    [HttpPost("restart")]
    public IActionResult Restart()
    {
        var zipPath = Path.Combine(TempDir, "update.zip");
        if (!System.IO.File.Exists(zipPath))
            return BusinessFail("未找到更新包，请先调用 /api/deploy/upload");

        LogOperation("执行热更新重启");

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000);
                var currentDir = _env.ContentRootPath;
                var flagFile = Path.Combine(currentDir, ".update-pending");
                await System.IO.File.WriteAllTextAsync(flagFile, zipPath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "热更新失败");
            }
        });

        return Success("更新包已就绪，服务即将重启");
    }
}
