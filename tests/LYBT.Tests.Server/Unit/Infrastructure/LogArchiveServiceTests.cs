using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using Xunit;

namespace LYBT.Tests.Server.Unit.Infrastructure;

/// <summary>
/// LogArchiveService 单元测试（F-08）——真实文件系统 + 真实 zip，零 mock。
/// 覆盖：按月分组归档、源文件删除、跳过当前月/近期文件、跳过被占用文件、重复执行幂等、禁用时 no-op、
/// 以及 BackgroundService 首次执行的实际装配。
/// </summary>
public class LogArchiveServiceTests : IDisposable
{
    private readonly string _logDirectory = Path.Combine(
        Path.GetTempPath(),
        $"lybt-logarchive-test-{Guid.NewGuid():N}");

    /// <summary>目标月份 = 两个月前（必定是「已结束月份」且 > ArchiveAfterDays 天）</summary>
    private static DateTime TargetMonth => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-2);

    private string ArchiveDirectory => Path.Combine(_logDirectory, "archive");

    public LogArchiveServiceTests() => Directory.CreateDirectory(_logDirectory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_logDirectory))
                Directory.Delete(_logDirectory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 临时目录清理失败不影响测试结论
        }
    }

    [Fact]
    public void ArchiveCompletedMonths_GroupsByMonthPrefix_AndDeletesSources()
    {
        var service = CreateService();

        var augustA = WriteLog("lybt-web-api", TargetMonth.AddDays(9), "A-content");
        var augustB = WriteLog("lybt-web-api", TargetMonth.AddDays(19), "B-content");
        var augustRolled = WriteLog("lybt-web-api", TargetMonth.AddDays(9), "B2-content", suffix: "_001");
        var bootstrap = WriteLog("bootstrap", TargetMonth.AddDays(14), "bootstrap-content");
        var currentMonth = WriteLog("lybt-web-api", DateTime.Now, "current-content");
        var recent = WriteLog("lybt-web-api", DateTime.Now.AddDays(-3), "recent-content");
        var otherPrefix = WriteLog("other-app", TargetMonth.AddDays(5), "other-content");

        var processed = service.ArchiveCompletedMonths();

        processed.Should().Be(4, "只归档配置前缀下、已结束月份且超过 ArchiveAfterDays 的文件");

        var apiZip = Path.Combine(ArchiveDirectory, $"lybt-web-api-{TargetMonth:yyyy-MM}.zip");
        var bootstrapZip = Path.Combine(ArchiveDirectory, $"bootstrap-{TargetMonth:yyyy-MM}.zip");
        File.Exists(apiZip).Should().BeTrue();
        File.Exists(bootstrapZip).Should().BeTrue();

        ReadEntries(apiZip).Should().BeEquivalentTo(
        [
            ($"lybt-web-api-{TargetMonth.AddDays(9):yyyyMMdd}.log", "A-content"),
            ($"lybt-web-api-{TargetMonth.AddDays(19):yyyyMMdd}.log", "B-content"),
            ($"lybt-web-api-{TargetMonth.AddDays(9):yyyyMMdd}_001.log", "B2-content")
        ]);
        ReadEntries(bootstrapZip).Should().BeEquivalentTo(
            [($"bootstrap-{TargetMonth.AddDays(14):yyyyMMdd}.log", "bootstrap-content")]);

        File.Exists(augustA).Should().BeFalse("归档成功后删除源文件");
        File.Exists(augustB).Should().BeFalse();
        File.Exists(augustRolled).Should().BeFalse();
        File.Exists(bootstrap).Should().BeFalse();

        File.Exists(currentMonth).Should().BeTrue("当前月的文件仍在写入");
        File.Exists(recent).Should().BeTrue("ArchiveAfterDays 内的文件不归档");
        File.Exists(otherPrefix).Should().BeTrue("非配置前缀的文件不参与归档");
    }

    [Fact]
    public void ArchiveCompletedMonths_IsIdempotent_WhenSourceReappears()
    {
        var service = CreateService();
        WriteLog("lybt-web-api", TargetMonth.AddDays(9), "A-content");
        WriteLog("lybt-web-api", TargetMonth.AddDays(19), "B-content");
        service.ArchiveCompletedMonths().Should().Be(2);

        var apiZip = Path.Combine(ArchiveDirectory, $"lybt-web-api-{TargetMonth:yyyy-MM}.zip");
        ReadEntries(apiZip).Should().HaveCount(2);

        // 模拟「源文件重新出现」（归档后又被写入/恢复）：重跑不得产生重复条目
        var reappeared = WriteLog("lybt-web-api", TargetMonth.AddDays(9), "A-content-changed");
        var second = service.ArchiveCompletedMonths();

        second.Should().Be(1);
        ReadEntries(apiZip).Should().HaveCount(2, "同名条目已存在时不重复写入");
        ReadEntries(apiZip).Should().Contain(($"lybt-web-api-{TargetMonth.AddDays(9):yyyyMMdd}.log", "A-content"),
            "已有条目内容保持不变");
        File.Exists(reappeared).Should().BeFalse("已存在条目视为归档完成，源文件仍应删除");
    }

    [Fact]
    public void ArchiveCompletedMonths_SkipsLockedFile_AndContinuesWithOthers()
    {
        var service = CreateService();
        var locked = WriteLog("lybt-web-api", TargetMonth.AddDays(9), "locked-content");
        var free = WriteLog("lybt-web-api", TargetMonth.AddDays(19), "free-content");

        int processed;
        using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            processed = service.ArchiveCompletedMonths();
        }

        processed.Should().Be(1);
        var apiZip = Path.Combine(ArchiveDirectory, $"lybt-web-api-{TargetMonth:yyyy-MM}.zip");
        ReadEntries(apiZip).Should().BeEquivalentTo(
            [($"lybt-web-api-{TargetMonth.AddDays(19):yyyyMMdd}.log", "free-content")]);
        File.Exists(locked).Should().BeTrue("被占用的文件本轮跳过，不删除");
        File.Exists(free).Should().BeFalse();

        // 占用释放后下一轮补齐
        service.ArchiveCompletedMonths().Should().Be(1);
        ReadEntries(apiZip).Should().HaveCount(2);
        File.Exists(locked).Should().BeFalse();
    }

    [Fact]
    public void ArchiveCompletedMonths_WithoutCandidates_DoesNotCreateArchiveDirectory()
    {
        var service = CreateService();
        var currentMonth = WriteLog("lybt-web-api", DateTime.Now, "current-content");

        service.ArchiveCompletedMonths().Should().Be(0);

        Directory.Exists(ArchiveDirectory).Should().BeFalse("无候选文件时不产生空归档目录");
        File.Exists(currentMonth).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNothing()
    {
        var options = CreateOptions();
        options.Archive.Enabled = false;
        var service = new LogArchiveService(NullLogger<LogArchiveService>.Instance, Options.Create(options));
        var stale = WriteLog("lybt-web-api", TargetMonth.AddDays(9), "A-content");

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Directory.Exists(ArchiveDirectory).Should().BeFalse("Enabled=false 时归档服务不执行任何归档");
        File.Exists(stale).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ArchivesOnFirstRun()
    {
        var options = CreateOptions();
        options.Archive.InitialDelayMinutes = 0;
        var service = new LogArchiveService(NullLogger<LogArchiveService>.Instance, Options.Create(options));
        WriteLog("lybt-web-api", TargetMonth.AddDays(9), "A-content");

        await service.StartAsync(CancellationToken.None);
        var zip = Path.Combine(ArchiveDirectory, $"lybt-web-api-{TargetMonth:yyyy-MM}.zip");
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!File.Exists(zip) && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        File.Exists(zip).Should().BeTrue("BackgroundService 启动后应完成首轮归档");
    }

    #region 辅助

    private LogArchiveService CreateService() =>
        new(NullLogger<LogArchiveService>.Instance, Options.Create(CreateOptions()));

    private LoggingOptions CreateOptions() => new()
    {
        Archive = new LogArchiveOptions
        {
            Enabled = true,
            ArchiveAfterDays = 7,
            ArchiveDirectory = "archive",
            DeleteSourceAfterArchive = true,
            IntervalHours = 168,
            InitialDelayMinutes = 0,
            FilePrefixes = ["lybt-web-api", "bootstrap"],
            LogDirectory = _logDirectory
        }
    };

    private string WriteLog(string prefix, DateTime date, string content, string suffix = "")
    {
        var path = Path.Combine(_logDirectory, $"{prefix}-{date:yyyyMMdd}{suffix}.log");
        File.WriteAllText(path, content);
        return path;
    }

    private static List<(string Name, string Content)> ReadEntries(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return zip.Entries
            .Select(entry =>
            {
                using var reader = new StreamReader(entry.Open());
                return (entry.Name, reader.ReadToEnd());
            })
            .ToList();
    }

    #endregion
}
