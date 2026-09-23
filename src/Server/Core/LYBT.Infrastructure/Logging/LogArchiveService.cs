using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LYBT.Infrastructure.Logging;

/// <summary>
/// 文件日志归档后台服务（F-08）
/// <para>
/// 把「已结束月份」的日志文件按月打包为 <c>{LogDirectory}/{ArchiveDirectory}/{filePrefix}-YYYY-MM.zip</c>，
/// 归档成功后删除源文件。与 <see cref="LogCleanupService"/>（数据库 SystemLog 表清理）互补——
/// 本服务只管文件侧，不触碰数据库。
/// </para>
/// <para>不变量：</para>
/// <list type="bullet">
/// <item>当前月 / <see cref="LogArchiveOptions.ArchiveAfterDays"/> 天内的文件不归档（仍在写入）</item>
/// <item>单个文件被占用（IOException）只跳过该文件，不中断整批</item>
/// <item>幂等：已存在的归档条目不会重复写入，重复执行结果一致</item>
/// <item>异常永不逃逸出循环（仅记录日志）</item>
/// </list>
/// </summary>
public partial class LogArchiveService : BackgroundService
{
    private readonly ILogger<LogArchiveService> _logger;
    private readonly LoggingOptions _options;

    public LogArchiveService(ILogger<LogArchiveService> logger, IOptions<LoggingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>日志文件名：{prefix}-{yyyyMMdd}[_NNN].log（_NNN 为 US-LOG-009 的多进程后缀）</summary>
    [GeneratedRegex(@"^(?<prefix>.+?)-(?<date>\d{8})(?:_\d{3})?\.log$", RegexOptions.IgnoreCase)]
    private static partial Regex LogFileNamePattern();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Archive;

        if (!options.Enabled)
        {
            _logger.LogInformation("日志归档服务已禁用");
            return;
        }

        _logger.LogInformation(
            "日志归档服务已启动 - 日志目录: {LogDirectory}, 归档目录: {ArchiveDirectory}, 仅归档 {ArchiveAfterDays} 天前的完整月份, 间隔: {IntervalHours}小时",
            options.LogDirectory,
            options.ArchiveDirectory,
            options.ArchiveAfterDays,
            options.IntervalHours);

        // 初始延迟，避免启动阶段（数据库初始化/首条日志写入）争抢磁盘
        if (!await DelayAsync(TimeSpan.FromMinutes(options.InitialDelayMinutes), stoppingToken))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ArchiveCompletedMonths(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志归档作业执行失败");
            }

            if (!await DelayAsync(TimeSpan.FromHours(options.IntervalHours), stoppingToken))
                break;
        }
    }

    /// <summary>可取消等待；返回 false 表示已请求停止</summary>
    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// 执行一次归档（后台循环调用；测试直接调用）。
    /// </summary>
    /// <returns>本次处理（写入或已存在条目）的日志文件数</returns>
    public int ArchiveCompletedMonths(CancellationToken cancellationToken = default)
    {
        var options = _options.Archive;
        var logDirectory = options.LogDirectory;

        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            _logger.LogDebug("日志目录不存在，跳过归档: {LogDirectory}", logDirectory);
            return 0;
        }

        var now = DateTime.Now;
        var cutoff = now.Date.AddDays(-options.ArchiveAfterDays);
        var currentMonth = MonthKey(now);
        var prefixes = options.FilePrefixes ?? [];

        // 只扫顶层：archive/ 子目录不在扫描范围内（天然排除已归档的 zip）
        var candidates = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly)
            .Select(TryDescribe)
            .Where(file => file is not null)
            .Select(file => file!)
            .Where(file => prefixes.Contains(file.Prefix, StringComparer.OrdinalIgnoreCase))
            .Where(file => file.Date < cutoff && MonthKey(file.Date) != currentMonth)
            .ToList();

        if (candidates.Count == 0)
        {
            _logger.LogDebug("没有需要归档的日志文件（截止日期: {CutoffDate:yyyy-MM-dd}）", cutoff);
            return 0;
        }

        var archiveDirectory = Path.Combine(logDirectory, options.ArchiveDirectory);
        Directory.CreateDirectory(archiveDirectory);

        var processed = 0;

        foreach (var month in candidates.GroupBy(file => (file.Prefix, Month: MonthKey(file.Date))))
        {
            var zipPath = Path.Combine(archiveDirectory, $"{month.Key.Prefix}-{month.Key.Month}.zip");

            // 归档完成后才删除源文件——先落盘再删，避免 zip 未刷盘时源文件已丢失
            var archivedSources = new List<string>();

            try
            {
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                {
                    foreach (var file in month)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var entryName = Path.GetFileName(file.Path);
                        try
                        {
                            if (zip.GetEntry(entryName) is null)
                            {
                                zip.CreateEntryFromFile(file.Path, entryName, CompressionLevel.Optimal);
                            }
                            else
                            {
                                _logger.LogDebug("归档条目已存在，跳过写入: {Entry}", entryName);
                            }
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            // 文件仍被日志写入方占用（共享写）或权限不足——跳过该文件，其余继续
                            _logger.LogWarning(ex, "日志文件不可读，跳过归档: {File}", file.Path);
                            continue;
                        }

                        archivedSources.Add(file.Path);
                        processed++;
                    }
                }

                _logger.LogInformation(
                    "日志归档完成 - {Zip}: {Count} 个文件",
                    zipPath,
                    archivedSources.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 停止请求：不记为失败（未完成的条目下次运行幂等补齐）
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志归档失败（zip 未写入，源文件保留）: {Zip}", zipPath);
                continue;
            }

            if (options.DeleteSourceAfterArchive)
            {
                DeleteArchivedSources(archivedSources);
            }
        }

        return processed;
    }

    private void DeleteArchivedSources(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 删除失败不影响归档结果（下次运行会因条目已存在而跳过写入，仅重试删除）
                _logger.LogWarning(ex, "归档成功但源文件删除失败: {File}", path);
            }
        }
    }

    private static string MonthKey(DateTime date) =>
        date.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private static LogFile? TryDescribe(string path)
    {
        var match = LogFileNamePattern().Match(Path.GetFileName(path));
        if (!match.Success)
            return null;

        if (!DateTime.TryParseExact(
                match.Groups["date"].Value,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            return null;

        return new LogFile(path, match.Groups["prefix"].Value, date);
    }

    private sealed record LogFile(string Path, string Prefix, DateTime Date);
}
