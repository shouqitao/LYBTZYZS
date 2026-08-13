namespace LYBT.WebAPI.Services;

/// <summary>
/// 下载主页服务（P1-6 2026-08-14: 文件扫描 + HTML 生成从 DownloadController 移出——Controller 仅编排）
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// 生成下载主页 HTML（扫描发布包目录 + 版本提取 + HTML 组装）
    /// </summary>
    string BuildDownloadPageHtml();
}
