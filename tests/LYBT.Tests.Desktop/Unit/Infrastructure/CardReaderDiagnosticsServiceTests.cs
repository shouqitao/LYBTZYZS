using LYBT.Desktop.Infrastructure.CardReader.Abstractions;
using LYBT.Desktop.Infrastructure.CardReader.Adapters;
using LYBT.Desktop.Infrastructure.CardReader.Services;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.Unit.Infrastructure;

/// <summary>
/// 读卡器诊断服务单测（SHELL-019: 探测/握手/读卡链路——Mock 驱动，不触真实硬件）
/// </summary>
public class CardReaderDiagnosticsServiceTests
{
    private readonly ICardReaderFactory _factory;
    private readonly CardReaderDiagnosticsService _service;

    public CardReaderDiagnosticsServiceTests()
    {
        _factory = Substitute.For<ICardReaderFactory>();
        var logger = Substitute.For<ILogger<CardReaderDiagnosticsService>>();
        _service = new CardReaderDiagnosticsService(_factory, logger);
    }

    [Fact]
    public async Task RunDiagnostics_WithConnectedMockReader_ReportsPassed()
    {
        var mock = new MockCardReader(new CardReaderOptions());
        _factory.CreateReader(CardReaderType.HuaDaHD100, Arg.Any<CardReaderOptions?>())
            .Returns(mock);

        var report = await _service.RunDiagnosticsAsync(CardReaderType.HuaDaHD100);

        report.DeviceDetected.Should().BeTrue();
        report.HandshakeOk.Should().BeTrue();
        report.Passed.Should().BeTrue();
        report.DeviceInfo.Should().NotBeNull();
        report.DeviceInfo!.Model.Should().Be("Mock-v1");
        report.Messages.Should().Contain(m => m.Contains("设备探测通过"));
    }

    [Fact]
    public async Task RunDiagnostics_WithDisconnectedReader_ReportsFailure()
    {
        var failingReader = Substitute.For<ICardReader>();
        failingReader.Name.Returns("失败读卡器");
        failingReader.ConnectAsync(Arg.Any<string?>()).Returns(false);
        _factory.CreateReader(CardReaderType.HuaDaHD100, Arg.Any<CardReaderOptions?>())
            .Returns(failingReader);

        var report = await _service.RunDiagnosticsAsync(CardReaderType.HuaDaHD100);

        report.DeviceDetected.Should().BeFalse();
        report.Passed.Should().BeFalse();
        report.ReadTestResult.Should().Contain("探测失败");
    }

    [Fact]
    public async Task RunDiagnostics_FactoryThrows_ReportsFailure()
    {
        _factory.CreateReader(CardReaderType.HuaDaHD100, Arg.Any<CardReaderOptions?>())
            .Returns(_ => throw new InvalidOperationException("驱动加载失败"));

        var report = await _service.RunDiagnosticsAsync(CardReaderType.HuaDaHD100);

        report.DeviceDetected.Should().BeFalse();
        report.Messages.Should().Contain(m => m.Contains("驱动加载失败"));
    }

    [Fact]
    public async Task RunDiagnostics_ManualOptions_ArePassedToFactory()
    {
        var mock = new MockCardReader(new CardReaderOptions());
        _factory.CreateReader(CardReaderType.HuaDaHD100, Arg.Any<CardReaderOptions?>())
            .Returns(mock);

        var options = new CardReaderOptions { UsbPort = 2001, ConnectTimeout = 3000, ReadTimeout = 8000 };
        await _service.RunDiagnosticsAsync(CardReaderType.HuaDaHD100, options);

        _factory.Received(1).CreateReader(
            CardReaderType.HuaDaHD100,
            Arg.Is<CardReaderOptions?>(o => o != null && o.UsbPort == 2001 && o.ConnectTimeout == 3000));
    }
}
