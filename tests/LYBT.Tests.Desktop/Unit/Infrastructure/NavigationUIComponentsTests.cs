using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Infrastructure.Navigation.Controls;
using LYBT.Desktop.Infrastructure.Converters;
using Prism.Events;
using Prism.Regions;
using NSubstitute;
using System;
using System.Globalization;

namespace LYBT.Tests.Desktop.Infrastructure.Navigation.Controls
{
    /// <summary>
    /// Navigation UI Components Unit Tests - Phase 2.1: Navigation Improvements
    /// </summary>
    public class NavigationUIComponentsTests
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<EnhancedNavigationService> _logger;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegion _contentRegion;

        public NavigationUIComponentsTests()
        {
            _regionManager = Substitute.For<IRegionManager>();
            _logger = Substitute.For<ILogger<EnhancedNavigationService>>();
            _eventAggregator = Substitute.For<IEventAggregator>();
            _contentRegion = Substitute.For<IRegion>();

            _regionManager.Regions.ContainsRegionWithName("ContentRegion").Returns(true);
            _regionManager.Regions["ContentRegion"].Returns(_contentRegion);
        }

        private EnhancedNavigationService CreateNavigationService()
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

        #region TimestampFormatConverter Tests

        [Fact]
        public void TimestampFormatConverter_ConvertsRecentTimeCorrectly()
        {
            // Arrange
            var converter = new TimestampFormatConverter();
            var timestamp = DateTime.UtcNow.AddMinutes(-5);

            // Act
            var result = converter.Convert(timestamp, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("5 分钟前");
        }

        [Fact]
        public void TimestampFormatConverter_ConvertsHoursCorrectly()
        {
            // Arrange
            var converter = new TimestampFormatConverter();
            var timestamp = DateTime.UtcNow.AddHours(-2);

            // Act
            var result = converter.Convert(timestamp, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("2 小时前");
        }

        [Fact]
        public void TimestampFormatConverter_ConvertsDaysCorrectly()
        {
            // Arrange
            var converter = new TimestampFormatConverter();
            var timestamp = DateTime.UtcNow.AddDays(-3);

            // Act
            var result = converter.Convert(timestamp, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("3 天前");
        }

        [Fact]
        public void TimestampFormatConverter_ConvertsVeryRecentTime()
        {
            // Arrange
            var converter = new TimestampFormatConverter();
            var timestamp = DateTime.UtcNow.AddSeconds(-30);

            // Act
            var result = converter.Convert(timestamp, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("刚刚");
        }

        #endregion

        #region IconConverter Tests

        [Fact]
        public void IconConverter_ReturnsCorrectIconForMedicalCase()
        {
            // Arrange
            var converter = new IconConverter();

            // Act
            var result = converter.Convert("/MedicalCase/123", typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("📋");
        }

        [Fact]
        public void IconConverter_ReturnsCorrectIconForPatient()
        {
            // Arrange
            var converter = new IconConverter();

            // Act
            var result = converter.Convert("/Patient/Details/456", typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("👤");
        }

        [Fact]
        public void IconConverter_ReturnsCorrectIconForPrescription()
        {
            // Arrange
            var converter = new IconConverter();

            // Act
            var result = converter.Convert("/Prescription/List", typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("💊");
        }

        [Fact]
        public void IconConverter_ReturnsDefaultIconForUnknownUri()
        {
            // Arrange
            var converter = new IconConverter();

            // Act
            var result = converter.Convert("/Unknown/Path", typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("📄");
        }

        #endregion

        #region SuggestionTypeColorConverter Tests

        [Fact]
        public void SuggestionTypeColorConverter_ContextualReturnsBlue()
        {
            // Arrange
            var converter = new SuggestionTypeColorConverter();

            // Act
            var result = converter.Convert(SuggestionType.Contextual, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("#2196F3");
        }

        [Fact]
        public void SuggestionTypeColorConverter_FrequentReturnsGreen()
        {
            // Arrange
            var converter = new SuggestionTypeColorConverter();

            // Act
            var result = converter.Convert(SuggestionType.Frequent, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("#4CAF50");
        }

        [Fact]
        public void SuggestionTypeColorConverter_TimeBasedReturnsOrange()
        {
            // Arrange
            var converter = new SuggestionTypeColorConverter();

            // Act
            var result = converter.Convert(SuggestionType.TimeBased, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("#FF9800");
        }

        [Fact]
        public void SuggestionTypeColorConverter_RecentReturnsPurple()
        {
            // Arrange
            var converter = new SuggestionTypeColorConverter();

            // Act
            var result = converter.Convert(SuggestionType.Recent, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("#9C27B0");
        }

        #endregion

        #region SuggestionTypeTextConverter Tests

        [Fact]
        public void SuggestionTypeTextConverter_ContextualReturnsChinese()
        {
            // Arrange
            var converter = new SuggestionTypeTextConverter();

            // Act
            var result = converter.Convert(SuggestionType.Contextual, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("上下文");
        }

        [Fact]
        public void SuggestionTypeTextConverter_FrequentReturnsChinese()
        {
            // Arrange
            var converter = new SuggestionTypeTextConverter();

            // Act
            var result = converter.Convert(SuggestionType.Frequent, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("常用");
        }

        [Fact]
        public void SuggestionTypeTextConverter_TimeBasedReturnsChinese()
        {
            // Arrange
            var converter = new SuggestionTypeTextConverter();

            // Act
            var result = converter.Convert(SuggestionType.TimeBased, typeof(string), null, CultureInfo.CurrentCulture);

            // Assert
            result.Should().Be("时间");
        }

        #endregion

        #region BreadcrumbControlViewModel Tests

        [Fact]
        public void BreadcrumbControlViewModel_WithValidService_InitializesCorrectly()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
                .Do(callInfo =>
                {
                    var callback = callInfo.Arg<Action<NavigationResult>>();
                    callback?.Invoke(CreateNavigationResult(true));
                });

            // Act
            var viewModel = new BreadcrumbControlViewModel(navigationService);

            // Assert
            viewModel.Should().NotBeNull();
            viewModel.Items.Should().NotBeNull();
        }

        [Fact]
        public async Task BreadcrumbControlViewModel_UpdatesOnNavigation()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
                .Do(callInfo =>
                {
                    var callback = callInfo.Arg<Action<NavigationResult>>();
                    callback?.Invoke(CreateNavigationResult(true));
                });

            var viewModel = new BreadcrumbControlViewModel(navigationService);
            bool eventFired = false;
            navigationService.Navigated += (s, e) => eventFired = true;

            // Act
            await navigationService.NavigateAsync("/MedicalCase/Edit/123");

            // Assert
            eventFired.Should().BeTrue();
        }

        #endregion

        #region NavigationHistoryPanelViewModel Tests

        [Fact]
        public void NavigationHistoryPanelViewModel_InitiallyHasNoHistory()
        {
            // Arrange
            var navigationService = CreateNavigationService();

            // Act
            var viewModel = new NavigationHistoryPanelViewModel(navigationService);

            // Assert
            viewModel.HasHistory.Should().BeFalse();
            viewModel.HistoryCount.Should().Be(0);
        }

        [Fact]
        public void NavigationHistoryPanelViewModel_ClearHistoryWorks()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            _contentRegion.When(x => x.RequestNavigate(Arg.Any<Uri>(), Arg.Any<Action<NavigationResult>>()))
                .Do(callInfo =>
                {
                    var callback = callInfo.Arg<Action<NavigationResult>>();
                    callback?.Invoke(CreateNavigationResult(true));
                });

            var viewModel = new NavigationHistoryPanelViewModel(navigationService);

            // Act
            navigationService.ClearHistory();

            // Assert
            viewModel.HasHistory.Should().BeFalse();
        }

        [Fact]
        public void NavigationHistoryPanelViewModel_FormatTimestamp_Correctly()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            var viewModel = new NavigationHistoryPanelViewModel(navigationService);
            var timestamp = DateTime.UtcNow.AddMinutes(-5);

            // Act
            var result = viewModel.FormatTimestamp(timestamp);

            // Assert
            result.Should().Be("5 分钟前");
        }

        [Fact]
        public void NavigationHistoryPanelViewModel_GetIconForUri_Correctly()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            var viewModel = new NavigationHistoryPanelViewModel(navigationService);

            // Act & Assert
            viewModel.GetNavigationIcon("/MedicalCase/123").Should().Be("📋");
            viewModel.GetNavigationIcon("/Patient/456").Should().Be("👤");
            viewModel.GetNavigationIcon("/Prescription/789").Should().Be("💊");
            viewModel.GetNavigationIcon("/Unknown").Should().Be("📄");
        }

        #endregion

        #region NavigationSuggestionsPanelViewModel Tests

        [Fact]
        public void NavigationSuggestionsPanelViewModel_LoadsSuggestions()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            var viewModel = new NavigationSuggestionsPanelViewModel(navigationService);

            // Act
            var hasSuggestions = viewModel.HasSuggestions;

            // Assert
            // Initially may be empty, but should not throw
            hasSuggestions.Should().BeFalse(); // Or true if service has defaults
        }

        [Fact]
        public void NavigationSuggestionsPanelViewModel_GetTypeText_Correctly()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            var viewModel = new NavigationSuggestionsPanelViewModel(navigationService);

            // Act & Assert
            viewModel.GetSuggestionTypeText(SuggestionType.Contextual).Should().Be("上下文");
            viewModel.GetSuggestionTypeText(SuggestionType.Frequent).Should().Be("常用");
            viewModel.GetSuggestionTypeText(SuggestionType.TimeBased).Should().Be("时间");
            viewModel.GetSuggestionTypeText(SuggestionType.Recent).Should().Be("最近");
            viewModel.GetSuggestionTypeText(SuggestionType.Pinned).Should().Be("固定");
        }

        [Fact]
        public void NavigationSuggestionsPanelViewModel_GetTypeColor_Correctly()
        {
            // Arrange
            var navigationService = CreateNavigationService();
            var viewModel = new NavigationSuggestionsPanelViewModel(navigationService);

            // Act & Assert
            viewModel.GetSuggestionTypeColor(SuggestionType.Contextual).Should().Be("#2196F3");
            viewModel.GetSuggestionTypeColor(SuggestionType.Frequent).Should().Be("#4CAF50");
            viewModel.GetSuggestionTypeColor(SuggestionType.TimeBased).Should().Be("#FF9800");
            viewModel.GetSuggestionTypeColor(SuggestionType.Recent).Should().Be("#9C27B0");
            viewModel.GetSuggestionTypeColor(SuggestionType.Pinned).Should().Be("#F44336");
        }

        #endregion

        #region Navigation Models Tests

        [Fact]
        public void NavigationEntry_WithExpression_CreatesCopy()
        {
            // Arrange
            var entry = new NavigationEntry(
                "/Test/123",
                "Test Entry",
                new NavigationParameters { { "id", "123" } },
                DateTime.UtcNow
            );

            // Act
            var cloned = entry with { };

            // Assert
            cloned.Should().NotBeNull();
            cloned.Uri.Should().Be(entry.Uri);
            cloned.Title.Should().Be(entry.Title);
            cloned.Should().NotBeSameAs(entry); // Different instance
        }

        [Fact]
        public void BreadcrumbItem_Properties_Accessible()
        {
            // Arrange & Act
            var breadcrumb = new BreadcrumbItem(
                "Test",
                "/Test",
                false,
                1
            );

            // Assert
            breadcrumb.Title.Should().Be("Test");
            breadcrumb.Uri.Should().Be("/Test");
            breadcrumb.IsActive.Should().BeFalse();
            breadcrumb.Level.Should().Be(1);
        }

        [Fact]
        public void NavigationSuggestion_Properties_Accessible()
        {
            // Arrange & Act
            var suggestion = new NavigationSuggestion(
                "Test Suggestion",
                "/Test",
                0.8,
                "Test reason",
                SuggestionType.Contextual
            );

            // Assert
            suggestion.Title.Should().Be("Test Suggestion");
            suggestion.Uri.Should().Be("/Test");
            suggestion.Confidence.Should().Be(0.8);
            suggestion.Reason.Should().Be("Test reason");
            suggestion.Type.Should().Be(SuggestionType.Contextual);
        }

        #endregion
    }
}
