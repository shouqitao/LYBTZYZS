using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Clinical.ViewModels.Workspace;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Desktop.PureLogic.Clinical;

public class CardReaderPureTests
{
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientIntegration;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IWorkspaceHost _host;
    private readonly ILoggerFactory _loggerFactory;

    public CardReaderPureTests()
    {
        _cardReaderService = Substitute.For<ICardReaderService>();
        _patientIntegration = Substitute.For<IPatientCardReaderIntegration>();
        _medicalCaseService = Substitute.For<IMedicalCaseService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _context = Substitute.For<IMedicalCaseWorkspaceContext>();
        _host = Substitute.For<IWorkspaceHost>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
    }

    private CardReaderViewModel CreateSut() => new(
        _cardReaderService, _patientIntegration, _medicalCaseService,
        _navigationCoordinator, _context, _host, _loggerFactory);

    #region MaskIdNumber (static, pure logic)

    [Theory]
    [InlineData("110101199001011234", "110101****1234")]
    [InlineData("1234567890", "123456****7890")]
    public void MaskIdNumber_masks_middle_digits(string input, string expected)
    {
        CardReaderViewModel.MaskIdNumber(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456789")] // Less than 10 chars
    public void MaskIdNumber_returns_input_when_short_or_null(string? input)
    {
        CardReaderViewModel.MaskIdNumber(input!).Should().Be(input);
    }

    #endregion

    #region Initial state

    [Fact]
    public void Default_StatusMessage_is_not_connected()
    {
        var sut = CreateSut();

        sut.StatusMessage.Should().NotBeNullOrEmpty();
        sut.IsReading.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_delegates_to_service()
    {
        _cardReaderService.IsConnected.Returns(true);
        var sut = CreateSut();

        sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void IsAutoReadEnabled_delegates_to_service()
    {
        _cardReaderService.IsAutoReadEnabled.Returns(true);
        var sut = CreateSut();

        sut.IsAutoReadEnabled.Should().BeTrue();
    }

    #endregion

    #region ToggleAutoRead

    [Fact]
    public void ToggleAutoRead_does_nothing_when_not_connected()
    {
        _cardReaderService.IsConnected.Returns(false);
        var sut = CreateSut();

        sut.ToggleAutoRead();

        _cardReaderService.DidNotReceive().StartAutoRead(Arg.Any<int>());
        _cardReaderService.DidNotReceive().StopAutoRead();
    }

    [Fact]
    public void ToggleAutoRead_starts_when_connected_and_not_auto()
    {
        _cardReaderService.IsConnected.Returns(true);
        _cardReaderService.IsAutoReadEnabled.Returns(false);
        var sut = CreateSut();

        sut.ToggleAutoRead();

        _cardReaderService.Received(1).StartAutoRead(500);
    }

    [Fact]
    public void ToggleAutoRead_stops_when_connected_and_auto()
    {
        _cardReaderService.IsConnected.Returns(true);
        _cardReaderService.IsAutoReadEnabled.Returns(true);
        var sut = CreateSut();

        sut.ToggleAutoRead();

        _cardReaderService.Received(1).StopAutoRead();
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_unsubscribes_and_stops_auto_read_if_enabled()
    {
        _cardReaderService.IsAutoReadEnabled.Returns(true);
        var sut = CreateSut();

        sut.Dispose();

        _cardReaderService.Received(1).StopAutoRead();
    }

    [Fact]
    public void Dispose_does_not_stop_auto_read_if_not_enabled()
    {
        _cardReaderService.IsAutoReadEnabled.Returns(false);
        var sut = CreateSut();

        sut.Dispose();

        _cardReaderService.DidNotReceive().StopAutoRead();
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        _cardReaderService.IsAutoReadEnabled.Returns(true);
        var sut = CreateSut();

        sut.Dispose();
        sut.Dispose(); // Second call should be no-op

        _cardReaderService.Received(1).StopAutoRead();
    }

    #endregion
}
