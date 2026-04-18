using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Desktop.Contracts.Services;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

/// <summary>
/// Phase 1.3/2.2: ToastService tests
/// Tests for toast notification service
/// </summary>
public class ToastServiceTests : UserJourneyTestBase
{
    public ToastServiceTests(UserJourneyFixture fixture) : base(fixture)
    {
        // Ensure WPF environment is initialized
        WpfTestHelper.InitializeWpf();
    }

    private ToastService CreateSut() => new();

    [Fact]
    public void Constructor_InitializesService()
    {
        var sut = CreateSut();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void ShowInfo_CreatesInfoToast()
    {
        var sut = CreateSut();

        // Note: Full testing requires WPF Application.Current.MainWindow
        // This test verifies the method can be called without throwing
        var exception = Record.Exception(() => sut.ShowInfo("Test info message"));

        // In test environment without main window, it should fall back to MessageBox or handle gracefully
        // We just verify it doesn't throw an unhandled exception
        exception.Should().BeNull();
    }

    [Fact]
    public void ShowSuccess_CreatesSuccessToast()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowSuccess("Test success message"));

        exception.Should().BeNull();
    }

    [Fact]
    public void ShowWarning_CreatesWarningToast()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowWarning("Test warning message"));

        exception.Should().BeNull();
    }

    [Fact]
    public void ShowError_CreatesErrorToast()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowError("Test error message"));

        exception.Should().BeNull();
    }

    [Fact]
    public void Show_WithCustomDuration_CreatesToast()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.Show("Test message", ToastType.Info, 5000));

        exception.Should().BeNull();
    }

    [Fact]
    public void ShowInfo_WithNullMessage_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowInfo(null!));

        // Should handle null gracefully (fallback to MessageBox)
        exception.Should().BeNull();
    }

    [Fact]
    public void ShowSuccess_WithEmptyMessage_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowSuccess(string.Empty));

        exception.Should().BeNull();
    }

    [Fact]
    public void ShowWarning_WithWhitespace_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() => sut.ShowWarning("   "));

        exception.Should().BeNull();
    }

    [Fact]
    public void ShowError_WithLongMessage_DoesNotThrow()
    {
        var sut = CreateSut();
        var longMessage = new string('A', 1000);

        var exception = Record.Exception(() => sut.ShowError(longMessage));

        exception.Should().BeNull();
    }

    [Fact]
    public void Show_WithAllToastTypes_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception1 = Record.Exception(() => sut.Show("Info", ToastType.Info));
        var exception2 = Record.Exception(() => sut.Show("Success", ToastType.Success));
        var exception3 = Record.Exception(() => sut.Show("Warning", ToastType.Warning));
        var exception4 = Record.Exception(() => sut.Show("Error", ToastType.Error));

        exception1.Should().BeNull();
        exception2.Should().BeNull();
        exception3.Should().BeNull();
        exception4.Should().BeNull();
    }

    [Fact]
    public void Show_WithDifferentDurations_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception1 = Record.Exception(() => sut.Show("Short", ToastType.Info, 1000));
        var exception2 = Record.Exception(() => sut.Show("Medium", ToastType.Info, 3000));
        var exception3 = Record.Exception(() => sut.Show("Long", ToastType.Info, 5000));
        var exception4 = Record.Exception(() => sut.Show("VeryLong", ToastType.Info, 10000));

        exception1.Should().BeNull();
        exception2.Should().BeNull();
        exception3.Should().BeNull();
        exception4.Should().BeNull();
    }

    [Fact]
    public void MultipleShowCalls_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = Record.Exception(() =>
        {
            sut.ShowInfo("Message 1");
            sut.ShowSuccess("Message 2");
            sut.ShowWarning("Message 3");
            sut.ShowError("Message 4");
        });

        exception.Should().BeNull();
    }
}
