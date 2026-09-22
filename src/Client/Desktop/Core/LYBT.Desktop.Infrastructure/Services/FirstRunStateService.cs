using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Primitives;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 首次运行状态服务（B-07：首次运行检测 + 完成标记）。
/// </summary>
/// <remarks>
/// <para>标记文件路径 <c>%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag</c>
/// （<see cref="AppDataPaths.DesktopDataDirectory"/>），与 <c>LoginViewModel</c> 的历史实现同一文件、
/// 同一内容格式（UTC ISO-8601）——升级后既有安装的「已完成」状态继续有效。</para>
/// <para>写入/删除均为 best-effort：IO 异常只记警告，绝不抛出（首次运行检测不得阻断启动）。</para>
/// <para>测试接缝：<see cref="DataDirectory"/> 为 <c>protected virtual</c>，单元测试可派生指向临时目录，
/// 避免污染真实用户数据（<b>不属于</b>冻结的 <see cref="IFirstRunStateService"/> 契约）。</para>
/// </remarks>
public class FirstRunStateService : IFirstRunStateService
{
    /// <summary>标记文件名（沿用既有布局）</summary>
    private const string MarkerFileName = "first_run_done.flag";

    private readonly ILogger<FirstRunStateService> _logger;

    /// <summary>构造服务（无文件 IO）</summary>
    public FirstRunStateService(ILogger<FirstRunStateService> logger)
    {
        _logger = logger;
    }

    /// <summary>数据目录（生产 = <see cref="AppDataPaths.DesktopDataDirectory"/>；测试可覆写指向临时目录）</summary>
    protected virtual string DataDirectory => AppDataPaths.DesktopDataDirectory;

    /// <inheritdoc />
    public string MarkerPath => Path.Combine(DataDirectory, MarkerFileName);

    /// <inheritdoc />
    public bool IsFirstRun => !File.Exists(MarkerPath);

    /// <inheritdoc />
    public void MarkCompleted()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(MarkerPath, DateTime.UtcNow.ToString("O"));
            _logger.LogInformation("[FIRST-RUN] 首次运行标记已写入：{Path}", MarkerPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FIRST-RUN] 写入首次运行标记失败（忽略，不阻断启动）");
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        try
        {
            var path = MarkerPath;
            if (!File.Exists(path))
                return;

            File.Delete(path);
            _logger.LogInformation("[FIRST-RUN] 首次运行标记已清除：{Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FIRST-RUN] 清除首次运行标记失败（忽略）");
        }
    }
}
