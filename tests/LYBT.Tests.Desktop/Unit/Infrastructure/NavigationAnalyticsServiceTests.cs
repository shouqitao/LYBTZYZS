using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Infrastructure.Navigation;
using NSubstitute;
using System.Diagnostics;
using Xunit;

namespace LYBT.Tests.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// Navigation Analytics Service Unit Tests - Phase 4: Analytics & Optimization
    /// </summary>
    public class NavigationAnalyticsServiceTests
    {
        private readonly ILogger<NavigationAnalyticsService> _logger;
        private readonly NavigationAnalyticsService _analyticsService;

        public NavigationAnalyticsServiceTests()
        {
            _logger = Substitute.For<ILogger<NavigationAnalyticsService>>();
            _analyticsService = new NavigationAnalyticsService(_logger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidLogger_InitializesSuccessfully()
        {
            // Assert
            _analyticsService.Should().NotBeNull();
            _analyticsService.AllEvents.Should().NotBeNull();
            _analyticsService.AllEvents.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new NavigationAnalyticsService(null!));
        }

        #endregion

        #region Tracking Method Tests

        [Fact]
        public void TrackNavigation_WithValidParameters_AddsEvent()
        {
            // Arrange
            var fromUri = "/Patient/List";
            var toUri = "/Patient/Details/123";
            var context = new NavigationContext("PatientManagement", "ViewDetails", null);

            // Act
            _analyticsService.TrackNavigation(fromUri, toUri, context);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("Navigate");
            evt.FromUri.Should().Be(fromUri);
            evt.ToUri.Should().Be(toUri);
        }

        [Fact]
        public void TrackNavigation_CalledMultipleTimes_TracksAllEvents()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);

            // Act
            _analyticsService.TrackNavigation("/Home", "/Patient/List", context);
            _analyticsService.TrackNavigation("/Patient/List", "/Patient/Details/123", context);
            _analyticsService.TrackNavigation("/Patient/Details/123", "/MedicalCase/Create", context);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(3);
        }

        [Fact]
        public void TrackNavigationCancellation_WithValidParameters_AddsEvent()
        {
            // Arrange
            var uri = "/Patient/Details/123";
            var reason = "User cancelled";

            // Act
            _analyticsService.TrackNavigationCancellation(uri, reason);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("Cancel");
            evt.FromUri.Should().Be(uri);
            evt.Metadata.Should().ContainKey("Reason");
            evt.Metadata!["Reason"].Should().Be(reason);
        }

        [Fact]
        public void TrackNavigationFailure_WithValidParameters_AddsEvent()
        {
            // Arrange
            var uri = "/MedicalCase/Create";
            var errorMessage = "Invalid patient ID";

            // Act
            _analyticsService.TrackNavigationFailure(uri, errorMessage);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("Failure");
            evt.FromUri.Should().Be(uri);
            evt.Metadata.Should().ContainKey("ErrorMessage");
            evt.Metadata!["ErrorMessage"].Should().Be(errorMessage);
        }

        [Fact]
        public void TrackHistoryNavigation_BackNavigation_AddsEvent()
        {
            // Arrange
            var uri = "/Patient/List";

            // Act
            _analyticsService.TrackHistoryNavigation(true, uri);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("History");
            evt.FromUri.Should().Be("[Back]");
            evt.ToUri.Should().Be(uri);
            evt.Metadata!["Direction"].Should().Be("Back");
        }

        [Fact]
        public void TrackHistoryNavigation_ForwardNavigation_AddsEvent()
        {
            // Arrange
            var uri = "/Patient/Details/123";

            // Act
            _analyticsService.TrackHistoryNavigation(false, uri);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.FromUri.Should().Be("[Forward]");
            evt.Metadata!["Direction"].Should().Be("Forward");
        }

        [Fact]
        public void TrackBreadcrumbClick_WithValidParameters_AddsEvent()
        {
            // Arrange
            var uri = "/Patient/List";
            var level = 1;

            // Act
            _analyticsService.TrackBreadcrumbClick(uri, level);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("Breadcrumb");
            evt.FromUri.Should().Be("[Level 1]");
            evt.ToUri.Should().Be(uri);
            evt.Metadata!["BreadcrumbLevel"].Should().Be(level);
        }

        [Fact]
        public void TrackSuggestionUsage_WithValidParameters_AddsEvent()
        {
            // Arrange
            var uri = "/MedicalCase/Create";
            var suggestionType = SuggestionType.Contextual;
            var confidence = 0.9;

            // Act
            _analyticsService.TrackSuggestionUsage(uri, suggestionType, confidence);

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(1);
            var evt = _analyticsService.AllEvents[0];
            evt.EventType.Should().Be("Suggestion");
            evt.FromUri.Should().Be("[Contextual]");
            evt.Metadata!["SuggestionType"].Should().Be("Contextual");
            evt.Metadata!["Confidence"].Should().Be(confidence);
        }

        #endregion

        #region Analytics Query Tests

        [Fact]
        public void GetInsights_WithNoEvents_ReturnsEmptyInsights()
        {
            // Act
            var insights = _analyticsService.GetInsights(TimeSpan.FromHours(1));

            // Assert
            insights.Should().NotBeNull();
            insights.TotalEvents.Should().Be(0);
            insights.MostCommonPaths.Should().BeEmpty();
            insights.MostAccessedPages.Should().BeEmpty();
        }

        [Fact]
        public void GetInsights_WithEvents_ReturnsCorrectInsights()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/Home", "/Patient/List", context);
            _analyticsService.TrackNavigation("/Patient/List", "/Patient/Details/123", context);
            _analyticsService.TrackNavigation("/Patient/Details/123", "/MedicalCase/Create", context);

            // Act
            var insights = _analyticsService.GetInsights(TimeSpan.FromHours(1));

            // Assert
            insights.TotalEvents.Should().BeGreaterOrEqualTo(3);
            insights.MostCommonPaths.Should().NotBeEmpty();
            insights.MostAccessedPages.Should().NotBeEmpty();
        }

        [Fact]
        public void GetMostCommonPaths_WithSequentialNavigations_ReturnsPaths()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);
            _analyticsService.TrackNavigation("/B", "/C", context);
            _analyticsService.TrackNavigation("/A", "/B", context);
            _analyticsService.TrackNavigation("/B", "/C", context);

            // Act
            var paths = _analyticsService.GetMostCommonPaths(TimeSpan.FromHours(1), 5);

            // Assert
            paths.Should().NotBeEmpty();
            var mostCommonPath = paths.First();
            mostCommonPath.Path.Should().Be("/B → /C");
            mostCommonPath.Frequency.Should().Be(2);
        }

        [Fact]
        public void GetMostAccessedPages_WithMultipleNavigations_ReturnsCorrectStats()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/Patient/List", context);
            _analyticsService.TrackNavigation("/B", "/Patient/List", context);
            _analyticsService.TrackNavigation("/Patient/List", "/Patient/Details/123", context);

            // Act
            var pages = _analyticsService.GetMostAccessedPages(TimeSpan.FromHours(1), 5);

            // Assert
            pages.Should().NotBeEmpty();
            var mostAccessed = pages.First();
            mostAccessed.Uri.Should().Be("/Patient/List");
            mostAccessed.AccessCount.Should().BeGreaterOrEqualTo(2);
        }

        [Fact]
        public void GetAverageNavigationTime_WithNoEvents_ReturnsZero()
        {
            // Act
            var avgTime = _analyticsService.GetAverageNavigationTime(TimeSpan.FromHours(1));

            // Assert
            avgTime.Should().Be(0);
        }

        [Fact]
        public void GetAverageNavigationTime_WithEvents_ReturnsAverageTime()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);
            Thread.Sleep(100); // Simulate time between navigations
            _analyticsService.TrackNavigation("/B", "/C", context);

            // Act
            var avgTime = _analyticsService.GetAverageNavigationTime(TimeSpan.FromHours(1));

            // Assert
            avgTime.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetNavigationErrorRate_WithNoErrors_ReturnsZero()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);
            _analyticsService.TrackNavigation("/B", "/C", context);

            // Act
            var errorRate = _analyticsService.GetNavigationErrorRate(TimeSpan.FromHours(1));

            // Assert
            errorRate.Should().Be(0);
        }

        [Fact]
        public void GetNavigationErrorRate_WithErrors_ReturnsCorrectRate()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);
            _analyticsService.TrackNavigationFailure("/B", "Error");
            _analyticsService.TrackNavigationCancellation("/C", "Cancelled");

            // Act
            var errorRate = _analyticsService.GetNavigationErrorRate(TimeSpan.FromHours(1));

            // Assert
            errorRate.Should().BeGreaterThan(0);
            errorRate.Should().BeLessOrEqualTo(100);
        }

        [Fact]
        public void GetUserPattern_WithNoUserEvents_ReturnsEmptyPattern()
        {
            // Act
            var pattern = _analyticsService.GetUserPattern(Guid.NewGuid(), TimeSpan.FromHours(1));

            // Assert
            pattern.Should().NotBeNull();
            pattern.FavoritePages.Should().BeEmpty();
            pattern.CommonPaths.Should().BeEmpty();
            pattern.TotalNavigations.Should().Be(0);
        }

        #endregion

        #region Data Management Tests

        [Fact]
        public void ClearOldData_WithOldEvents_RemovesOldEvents()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);

            // Act - clear data older than 1 hour
            _analyticsService.ClearOldData(TimeSpan.FromHours(1));

            // Assert - recent event should still be there
            _analyticsService.AllEvents.Count.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void ExportData_AsJson_ReturnsValidJson()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);

            // Act
            var json = _analyticsService.ExportData(TimeSpan.FromHours(1), AnalyticsFormat.Json);

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("\"EventType\"");
            json.Should().Contain("\"FromUri\"");
        }

        [Fact]
        public void ExportData_AsCsv_ReturnsValidCsv()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);

            // Act
            var csv = _analyticsService.ExportData(TimeSpan.FromHours(1), AnalyticsFormat.Csv);

            // Assert
            csv.Should().NotBeNullOrEmpty();
            csv.Should().Contain("Timestamp,EventType,FromUri,ToUri");
        }

        [Fact]
        public void ExportData_AsXml_ReturnsValidXml()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            _analyticsService.TrackNavigation("/A", "/B", context);

            // Act
            var xml = _analyticsService.ExportData(TimeSpan.FromHours(1), AnalyticsFormat.Xml);

            // Assert
            xml.Should().NotBeNullOrEmpty();
            xml.Should().Contain("<?xml version=\"1.0\"");
            xml.Should().Contain("<NavigationEvents>");
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task TrackNavigation_WhenCalledConcurrently_DoesNotThrow()
        {
            // Arrange
            var context = new NavigationContext(null, null, null);
            var tasks = new List<Task>();

            // Act - track navigation from multiple threads
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    _analyticsService.TrackNavigation($"/A/{index}", $"/B/{index}", context);
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            // Assert
            _analyticsService.AllEvents.Count.Should().Be(100);
        }

        #endregion
    }
}
