using System.IO;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Primitives;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Desktop;

/// <summary>
/// B-07 首次运行状态服务单测：标记路径（沿用既有 first_run_done.flag）/ 检测 / 标记 / 重置。
/// 真实文件系统（临时目录），Dispose 清理。
/// </summary>
public class FirstRunStateServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public FirstRunStateServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "lybt-firstrun-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (Exception)
        {
            // best-effort 清理
        }
    }

    /// <summary>测试接缝：数据目录指向临时目录（<see cref="FirstRunStateService.DataDirectory"/> 为 protected virtual）</summary>
    private sealed class TempDirectoryFirstRunStateService : FirstRunStateService
    {
        private readonly string _directory;

        public TempDirectoryFirstRunStateService(string directory)
            : base(NullLogger<FirstRunStateService>.Instance)
        {
            _directory = directory;
        }

        protected override string DataDirectory => _directory;
    }

    private FirstRunStateService CreateService() => new TempDirectoryFirstRunStateService(_tempDirectory);

    [Fact]
    public void MarkerPath_UsesLegacyFileNameInsideDataDirectory()
    {
        var service = CreateService();

        service.MarkerPath.Should().Be(Path.Combine(_tempDirectory, "first_run_done.flag"));
        service.MarkerPath.Should().EndWith("first_run_done.flag");
    }

    [Fact]
    public void MarkerPath_ForProductionService_UsesSharedAppDataDirectory()
    {
        // 既有安装的「已完成」状态必须继续有效——路径与 AppDataPaths SSOT 及 LoginViewModel 历史实现一致
        var service = new FirstRunStateService(NullLogger<FirstRunStateService>.Instance);

        service.MarkerPath.Should().Be(Path.Combine(AppDataPaths.DesktopDataDirectory, "first_run_done.flag"));
    }

    [Fact]
    public void MarkCompleted_ThenReset_TogglesIsFirstRunAndWritesTimestampContent()
    {
        var service = CreateService();

        service.IsFirstRun.Should().BeTrue();

        service.MarkCompleted();

        service.IsFirstRun.Should().BeFalse();
        File.Exists(service.MarkerPath).Should().BeTrue();
        File.ReadAllText(service.MarkerPath).Should().NotBeNullOrWhiteSpace();

        service.Reset();

        service.IsFirstRun.Should().BeTrue();
        File.Exists(service.MarkerPath).Should().BeFalse();
    }

    [Fact]
    public void MarkCompleted_CreatesMissingDataDirectory()
    {
        var nested = Path.Combine(_tempDirectory, "nested", "deeper");
        var service = new TempDirectoryFirstRunStateService(nested);

        var act = () => service.MarkCompleted();

        act.Should().NotThrow();
        service.IsFirstRun.Should().BeFalse();
    }

    [Fact]
    public void Reset_WithNoMarker_DoesNotThrow()
    {
        var service = CreateService();

        var act = () => service.Reset();

        act.Should().NotThrow();
        service.IsFirstRun.Should().BeTrue();
    }
}
