using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Infrastructure.Navigation;
using Prism.Events;
using Prism.Regions;
using NSubstitute;

namespace LYBT.Tests.Desktop.Infrastructure.Navigation;

/// <summary>
/// EnhancedNavigationService 单元测试
/// Phase 2.1: Navigation Improvements - Foundation Layer
/// </summary>
public class EnhancedNavigationServiceTests
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<EnhancedNavigationService> _logger;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegion _contentRegion;

    public EnhancedNavigationServiceTests()
    {
        // Arrange - 创建所有 mock
        _regionManager = Substitute.For<IRegionManager>();
        _logger = Substitute.For<ILogger<EnhancedNavigationService>>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _contentRegion = Substitute.For<IRegion>();

        // 设置 RegionManager 返回 mock region
        _regionManager.Regions.ContainsRegionWithName("ContentRegion").Returns(true);
        _regionManager.Regions["ContentRegion"].Returns(_contentRegion);
    }

    private EnhancedNavigationService CreateSut()
    {
        return new EnhancedNavigationService(_regionManager, _logger, _eventAggregator);
    }

    /// <summary>
    /// Creates a mock NavigationResult with the specified result value
    /// </summary>
    private NavigationResult CreateNavigationResult(bool result)
    {
        var navResult = Substitute.For<NavigationResult>();
        navResult.Result.Returns(result);
        return navResult;
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullRegionManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EnhancedNavigationService(null!, _logger, _eventAggregator));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EnhancedNavigationService(_regionManager, null!, _eventAggregator));
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesSuccessfully()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Should().NotBeNull();
        sut.History.Should().NotBeNull();
        sut.ForwardStack.Should().NotBeNull();
        sut.Breadcrumbs.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesWithEmptyHistory()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.History.Count.Should().Be(0);
        sut.CanGoBack.Should().BeFalse();
        sut.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesWithEmptyForwardStack()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.ForwardStack.Count.Should().Be(0);
        sut.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesWithEmptyBreadcrumbs()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Breadcrumbs.Count.Should().Be(0);
    }

    #endregion

    #region CurrentEntry

    [Fact]
    public void CurrentEntry_Initially_ReturnsHomeEntry()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var currentEntry = sut.CurrentEntry;

        // Assert
        currentEntry.Should().NotBeNull();
        currentEntry.Uri.Should().Be("/");
        currentEntry.Title.Should().Be("Home");
    }

    #endregion

    #region NavigateAsync

    [Fact]
    public async Task NavigateAsync_WithSimpleUri_NavigatesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var uri = "/MedicalCase";

        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act
        var result = await sut.NavigateAsync(uri);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateAsync_WithParameters_NavigatesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var uri = "/MedicalCase";
        var parameters = new NavigationParameters { { "id", "123" } };

        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act
        var result = await sut.NavigateAsync(uri, parameters);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateAsync_AddsToHistory()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - First navigation
        await sut.NavigateAsync("/MedicalCase");

        // Assert
        sut.History.Count.Should().Be(0); // First navigation doesn't add to history (nothing to go back from)

        // Act - Second navigation
        await sut.NavigateAsync("/Patient");

        // Assert
        sut.History.Count.Should().Be(1);
        sut.History[0].Uri.Should().Be("/MedicalCase");
        sut.CanGoBack.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateAsync_ClearsForwardStack()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Navigate twice
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");

        // Go back
        await sut.GoBackAsync();

        // Navigate to new location (should clear forward stack)
        await sut.NavigateAsync("/Prescription");

        // Assert
        sut.ForwardStack.Count.Should().Be(0);
        sut.CanGoForward.Should().BeFalse();
    }

    [Fact]
    public async Task NavigateAsync_UpdatesBreadcrumbs()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act
        await sut.NavigateAsync("/MedicalCase/Edit/123");

        // Assert
        sut.Breadcrumbs.Count.Should().BeGreaterThanOrEqualTo(2);
        sut.Breadcrumbs[^1].IsActive.Should().BeTrue();
        sut.Breadcrumbs[^1].Uri.Should().Be("/MedicalCase/Edit/123");
    }

    #endregion

    #region NavigateToRegionAsync

    [Fact]
    public async Task NavigateToRegionAsync_WithValidParameters_NavigatesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        string? capturedUri = null;

        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                capturedUri = callInfo.Arg<Uri>().OriginalString;
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act
        var result = await sut.NavigateToRegionAsync("ContentRegion", "MedicalCase");

        // Assert
        result.Should().BeTrue();
        capturedUri.Should().Contain("MedicalCase");
    }

    [Fact]
    public async Task NavigateToRegionAsync_WithInvalidRegion_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _regionManager.Regions.ContainsRegionWithName("InvalidRegion").Returns(false);

        // Act
        var result = await sut.NavigateToRegionAsync("InvalidRegion", "MedicalCase");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GoBackAsync

    [Fact]
    public async Task GoBackAsync_WithEmptyHistory_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GoBackAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GoBackAsync_WithHistory_NavigatesToPreviousEntry()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Navigate twice to build history
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");

        var historyCount = sut.History.Count;
        sut.CanGoBack.Should().BeTrue();

        // Go back
        var result = await sut.GoBackAsync();

        // Assert
        result.Should().BeTrue();
        sut.History.Count.Should().Be(historyCount - 1);
        sut.CurrentEntry.Uri.Should().Be("/MedicalCase");
    }

    [Fact]
    public async Task GoBackAsync_AddsCurrentEntryToForwardStack()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Navigate three times
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");
        await sut.NavigateAsync("/Prescription");

        // Go back
        await sut.GoBackAsync();

        // Assert
        sut.ForwardStack.Count.Should().Be(1);
        sut.ForwardStack[0].Uri.Should().Be("/Prescription");
        sut.CanGoForward.Should().BeTrue();
    }

    #endregion

    #region GoForwardAsync

    [Fact]
    public async Task GoForwardAsync_WithEmptyForwardStack_ReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.GoForwardAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GoForwardAsync_WithForwardStack_NavigatesToNextEntry()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Navigate three times, go back once
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");
        await sut.NavigateAsync("/Prescription");
        await sut.GoBackAsync();

        var forwardCount = sut.ForwardStack.Count;
        sut.CanGoForward.Should().BeTrue();

        // Go forward
        var result = await sut.GoForwardAsync();

        // Assert
        result.Should().BeTrue();
        sut.ForwardStack.Count.Should().Be(forwardCount - 1);
        sut.CurrentEntry.Uri.Should().Be("/Prescription");
    }

    #endregion

    #region ClearHistory

    [Fact]
    public async Task ClearHistory_ClearsAllNavigationHistory()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Build up history
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");

        sut.History.Count.Should().BeGreaterThan(0);

        sut.ClearHistory();

        // Assert
        sut.History.Count.Should().Be(0);
        sut.ForwardStack.Count.Should().Be(0);
        sut.CanGoBack.Should().BeFalse();
        sut.CanGoForward.Should().BeFalse();
    }

    #endregion

    #region GetSuggestions

    [Fact]
    public async Task GetSuggestions_ReturnsSuggestions()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        // Act - Navigate to build history
        await sut.NavigateAsync("/MedicalCase");
        await sut.NavigateAsync("/Patient");
        await sut.NavigateAsync("/MedicalCase"); // Visit MedicalCase again

        var suggestions = sut.GetSuggestions(5);

        // Assert
        suggestions.Should().NotBeNull();
        suggestions.Should().NotBeEmpty();
    }

    [Fact]
    public void GetSuggestions_RespectsCountParameter()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var suggestions3 = sut.GetSuggestions(3);
        var suggestions5 = sut.GetSuggestions(5);

        // Assert
        suggestions3.Count().Should().BeLessOrEqualTo(3);
        suggestions5.Count().Should().BeLessOrEqualTo(5);
    }

    #endregion

    #region Navigation Models

    [Fact]
    public void NavigationEntry_CanBeCreated()
    {
        // Arrange & Act
        var entry = new NavigationEntry(
            "/MedicalCase/123",
            "医案详情",
            new NavigationParameters(),
            DateTime.UtcNow
        );

        // Assert
        entry.Should().NotBeNull();
        entry.Uri.Should().Be("/MedicalCase/123");
        entry.Title.Should().Be("医案详情");
    }

    [Fact]
    public void BreadcrumbItem_CanBeCreated()
    {
        // Arrange & Act
        var breadcrumb = new BreadcrumbItem(
            "医案管理",
            "/MedicalCase",
            false,
            1
        );

        // Assert
        breadcrumb.Should().NotBeNull();
        breadcrumb.Title.Should().Be("医案管理");
        breadcrumb.Uri.Should().Be("/MedicalCase");
        breadcrumb.IsActive.Should().BeFalse();
        breadcrumb.Level.Should().Be(1);
    }

    [Fact]
    public void NavigationSuggestion_CanBeCreated()
    {
        // Arrange & Act
        var suggestion = new NavigationSuggestion(
            "查看患者历史",
            "/Patient/History",
            0.9,
            "看完诊后通常查看历史",
            SuggestionType.Contextual
        );

        // Assert
        suggestion.Should().NotBeNull();
        suggestion.Title.Should().Be("查看患者历史");
        suggestion.Confidence.Should().Be(0.9);
        suggestion.Type.Should().Be(SuggestionType.Contextual);
    }

    #endregion

    #region Events

    [Fact]
    public async Task Navigated_EventFiresOnSuccessfulNavigation()
    {
        // Arrange
        var sut = CreateSut();
        _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
            .Do(callInfo =>
            {
                var callback = callInfo.Arg<Action<NavigationResult>>();
                callback?.Invoke(CreateNavigationResult(true));
            });

        NavigatedEventArgs? capturedArgs = null;
        sut.Navigated += (s, e) => capturedArgs = e;

        // Act
        await sut.NavigateAsync("/MedicalCase");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs?.Entry.Uri.Should().Be("/MedicalCase");
        capturedArgs?.IsBack.Should().BeFalse();
        capturedArgs?.IsForward.Should().BeFalse();
    }

    #endregion
}
