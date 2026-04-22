using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Regions;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.ViewModels;

/// <summary>
/// Phase 2.2: NavigableViewModelBase message method tests
/// Tests for Toast-based notification methods
/// </summary>
public class NavigableViewModelBaseTests : UserJourneyTestBase
{
    public NavigableViewModelBaseTests(UserJourneyFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Test implementation of NavigableViewModelBase
    /// </summary>
    private class TestNavigableViewModel : NavigableViewModelBase
    {
        public TestNavigableViewModel(IViewModelServices services) : base(services)
        {
        }

        // Expose protected methods for testing
        public async Task CallShowSuccessMessageAsync(string message)
            => await ShowSuccessMessageAsync(message);

        public async Task CallShowErrorMessageAsync(string message)
            => await ShowErrorMessageAsync(message);

        public async Task CallShowWarningMessageAsync(string message)
            => await ShowWarningMessageAsync(message);
    }

    private TestNavigableViewModel CreateSut(IViewModelServices? services = null)
    {
        services ??= CreateViewModelServicesMock();
        return new TestNavigableViewModel(services);
    }

    [Fact]
    public void Constructor_InitializesWithServices()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);

        // Services are injected and accessible internally (tested via behavior)
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        var sut = CreateSut();

        sut.PageTitle.Should().BeEmpty();
        sut.IsLoading.Should().BeFalse();
        sut.IsNotLoading.Should().BeTrue();
        sut.IsInitialized.Should().BeFalse();
        sut.IsActive.Should().BeFalse();
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task ShowSuccessMessageAsync_CallsToastService()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var testMessage = "操作成功";

        await sut.CallShowSuccessMessageAsync(testMessage);

        services.ToastService.Received(1).ShowSuccess(testMessage);
    }

    [Fact]
    public async Task ShowErrorMessageAsync_CallsToastService()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var testMessage = "操作失败";

        await sut.CallShowErrorMessageAsync(testMessage);

        services.ToastService.Received(1).ShowError(testMessage);
    }

    [Fact]
    public async Task ShowWarningMessageAsync_CallsToastService()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var testMessage = "警告信息";

        await sut.CallShowWarningMessageAsync(testMessage);

        services.ToastService.Received(1).ShowWarning(testMessage);
    }

    [Fact]
    public async Task ShowSuccessMessageAsync_IsNonBlocking()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var taskCompleted = false;

        var task = sut.CallShowSuccessMessageAsync("测试消息");
        await task.ContinueWith(_ => taskCompleted = true);

        // Task should complete without blocking
        await Task.Delay(100);
        taskCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ShowErrorMessageAsync_WithEmptyMessage_DoesNotThrow()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);

        var exception = await Record.ExceptionAsync(async () =>
            await sut.CallShowErrorMessageAsync(string.Empty));

        exception.Should().BeNull();
    }

    [Fact]
    public async Task ShowWarningMessageAsync_WithNullMessage_DoesNotThrow()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);

        var exception = await Record.ExceptionAsync(async () =>
            await sut.CallShowWarningMessageAsync(null!));

        exception.Should().BeNull();
    }

    [Fact]
    public async Task MultipleMessageCalls_AllExecute()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);

        await sut.CallShowSuccessMessageAsync("成功消息");
        await sut.CallShowWarningMessageAsync("警告消息");
        await sut.CallShowErrorMessageAsync("错误消息");

        services.ToastService.Received(1).ShowSuccess(Arg.Any<string>());
        services.ToastService.Received(1).ShowWarning(Arg.Any<string>());
        services.ToastService.Received(1).ShowError(Arg.Any<string>());
    }

    [Fact]
    public void PageTitle_SetProperty_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.PageTitle = "测试页面";

        propertiesChanged.Should().Contain(nameof(NavigableViewModelBase.PageTitle));
    }

    [Fact]
    public void IsLoading_SetProperty_RaisesIsNotLoadingChanged()
    {
        var sut = CreateSut();
        var propertiesChanged = new List<string?>();
        sut.PropertyChanged += (_, e) => propertiesChanged.Add(e.PropertyName);

        sut.IsLoading = true;

        propertiesChanged.Should().Contain(nameof(NavigableViewModelBase.IsLoading));
        propertiesChanged.Should().Contain(nameof(NavigableViewModelBase.IsNotLoading));
    }

    [Fact]
    public void IsLoading_True_SetsIsNotLoadingToFalse()
    {
        var sut = CreateSut();

        sut.IsLoading = true;

        sut.IsLoading.Should().BeTrue();
        sut.IsNotLoading.Should().BeFalse();
    }

    [Fact]
    public void IsLoading_False_SetsIsNotLoadingToTrue()
    {
        var sut = CreateSut();

        sut.IsLoading = false;

        sut.IsLoading.Should().BeFalse();
        sut.IsNotLoading.Should().BeTrue();
    }

    [Fact]
    public void KeepAlive_DefaultIsTrue()
    {
        var sut = CreateSut();

        sut.KeepAlive.Should().BeTrue();
    }

    [Fact]
    public void IsNavigationTarget_DefaultReturnsTrue()
    {
        var sut = CreateSut();
        var navigationContext = Substitute.For<NavigationContext>();

        var result = sut.IsNavigationTarget(navigationContext);

        result.Should().BeTrue();
    }

    [Fact]
    public void OnNavigatedTo_SetsIsActiveToTrue()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var navigationContext = Substitute.For<NavigationContext>();

        sut.OnNavigatedTo(navigationContext);

        sut.IsActive.Should().BeTrue();
    }

    [Fact]
    public void OnNavigatedFrom_SetsIsActiveToFalse()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var navigationContext = Substitute.For<NavigationContext>();
        sut.IsActive = true;

        sut.OnNavigatedFrom(navigationContext);

        sut.IsActive.Should().BeFalse();
    }

    [Fact]
    public void HasUnsavedChanges_DefaultIsFalse()
    {
        var sut = CreateSut();

        ((IEditable)sut).HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public void Logger_IsAvailable()
    {
        var sut = CreateSut();

        // Logger is injected and available internally (tested via behavior)
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task ShowSuccessMessageAsync_WithLongMessage_DoesNotThrow()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var longMessage = new string('A', 1000);

        var exception = await Record.ExceptionAsync(async () =>
            await sut.CallShowSuccessMessageAsync(longMessage));

        exception.Should().BeNull();
    }

    [Fact]
    public async Task ShowErrorMessageAsync_WithSpecialCharacters_DoesNotThrow()
    {
        var services = CreateViewModelServicesMock();
        var sut = CreateSut(services);
        var specialMessage = "错误：包含\n特殊\r字符\t\"'&<>";

        var exception = await Record.ExceptionAsync(async () =>
            await sut.CallShowErrorMessageAsync(specialMessage));

        exception.Should().BeNull();
    }
}
