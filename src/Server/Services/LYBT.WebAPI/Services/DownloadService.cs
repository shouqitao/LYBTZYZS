using Microsoft.Extensions.Configuration;

namespace LYBT.WebAPI.Services;

/// <summary>
/// 下载主页服务实现（P1-6 2026-08-14: ExtractVersion/BuildHtml/FormatSize 从 DownloadController 移入——
/// Controller 仅调用，业务生成逻辑归 Service）
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public DownloadService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public string BuildDownloadPageHtml()
    {
        var enabled = _configuration.GetValue<bool>("DesktopUpdate:Enabled");
        var baseUrl = _configuration["DesktopUpdate:DownloadBaseUrl"] ?? "/releases";
        var releasesPath = _configuration["DesktopUpdate:ReleasesPath"];
        // SWAGGER-TOGGLE: 主页提供 API 文档入口——与中间件同一开关判定（SwaggerAvailability SSOT，防死链）
        var swaggerEnabled = LYBT.WebAPI.Configuration.SwaggerAvailability.IsEnabled(_configuration, _environment);

        var files = new List<(string Name, long Size, DateTime Modified)>();
        string? version = null;

        if (enabled && !string.IsNullOrEmpty(releasesPath) && Directory.Exists(releasesPath))
        {
            foreach (var file in Directory.GetFiles(releasesPath))
            {
                var fi = new FileInfo(file);
                var name = fi.Name;
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("RELEASES", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add((name, fi.Length, fi.LastWriteTimeUtc));
                    if (version is null && name.StartsWith("Setup", StringComparison.OrdinalIgnoreCase))
                        version = ExtractVersion(name);
                }
            }
            files = files.OrderByDescending(f => f.Modified).ToList();
        }

        return BuildHtml(enabled, baseUrl, version, files, swaggerEnabled);
    }

    private static string? ExtractVersion(string fileName)
    {
        var core = Path.GetFileNameWithoutExtension(fileName)
            .Replace("Setup", "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('-', ' ');
        return string.IsNullOrWhiteSpace(core) ? null : core;
    }

    private static string BuildHtml(bool enabled, string baseUrl, string? version, List<(string Name, long Size, DateTime Modified)> files, bool swaggerEnabled)
    {
        var rows = files.Count == 0
            ? "<p style=\"opacity:0.6\">暂无发布包（等待管理员上传）</p>"
            : string.Join("\n", files.Select(f =>
                "<li><a href=\"" + baseUrl.TrimEnd('/') + "/" + Uri.EscapeDataString(f.Name)
                + "\" style=\"text-decoration:none;color:#1a73e8\">"
                + System.Net.WebUtility.HtmlEncode(f.Name)
                + "</a> <span style=\"opacity:0.6;font-size:12px\">(" + FormatSize(f.Size)
                + " · " + f.Modified.ToString("yyyy-MM-dd HH:mm") + " UTC)</span></li>"));

        var versionText = version ?? "未知";
        var statusText = enabled ? "更新服务已启用" : "更新服务未启用";
        var exeUrl = baseUrl.TrimEnd('/') + "/Setup.exe";

        return "<!DOCTYPE html>\n"
            + "<html lang=\"zh-CN\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
            + "<title>凌隐宝堂中医诊所 · 桌面客户端下载</title>"
            + "<style>body{font-family:\"Microsoft YaHei\",sans-serif;background:#f5f7fa;margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh}"
            + ".card{background:#fff;border-radius:12px;box-shadow:0 4px 20px rgba(0,0,0,.08);padding:40px 48px;max-width:560px;width:100%}"
            + "h1{font-size:22px;color:#1a2332;margin:0 0 8px}.sub{color:#5f6b7a;font-size:14px;margin-bottom:28px}"
            + ".btn{display:inline-block;background:#1a73e8;color:#fff;padding:12px 32px;border-radius:6px;text-decoration:none;font-size:15px}"
            + ".btn:hover{background:#1557b0}.meta{margin-top:24px;font-size:13px;color:#5f6b7a;border-top:1px solid #eee;padding-top:16px}"
            + ".meta span{margin-right:20px}ul{list-style:none;padding:0;margin:20px 0 0}li{padding:8px 0;border-bottom:1px solid #f0f0f0}"
            + "</style></head><body><div class=\"card\">"
            + "<h1>凌隐宝堂中医诊所 · 桌面客户端下载</h1>"
            + "<div class=\"sub\">下载安装包后运行 Setup.exe 即可完成安装（免管理员权限）</div>"
            + "<a class=\"btn\" href=\"" + exeUrl + "\">下载桌面客户端</a>"
            + (swaggerEnabled
                ? "<a class=\"btn\" href=\"/swagger\" style=\"background:#5f6b7a;margin-left:12px\">API 文档</a>"
                : "")
            + "<div class=\"meta\"><span>版本：" + versionText + "</span><span>" + statusText + "</span></div>"
            + "<ul>" + rows + "</ul>"
            + "</div></body></html>";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024) return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
        if (bytes >= 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
        return bytes + " B";
    }
}
