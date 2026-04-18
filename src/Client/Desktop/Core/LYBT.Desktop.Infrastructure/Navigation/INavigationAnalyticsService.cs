using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// Navigation Analytics Service Interface - Phase 4: Analytics & Optimization
    /// Track and analyze navigation patterns for continuous improvement
    /// </summary>
    public interface INavigationAnalyticsService
    {
        #region Tracking Methods

        /// <summary>
        /// Track a navigation event
        /// </summary>
        void TrackNavigation(string fromUri, string toUri, NavigationContext context);

        /// <summary>
        /// Track navigation cancellation
        /// </summary>
        void TrackNavigationCancellation(string uri, string reason);

        /// <summary>
        /// Track navigation failure
        /// </summary>
        void TrackNavigationFailure(string uri, string errorMessage);

        /// <summary>
        /// Track back/forward navigation
        /// </summary>
        void TrackHistoryNavigation(bool isBack, string uri);

        /// <summary>
        /// Track breadcrumb click
        /// </summary>
        void TrackBreadcrumbClick(string uri, int level);

        /// <summary>
        /// Track suggestion usage
        /// </summary>
        void TrackSuggestionUsage(string uri, SuggestionType suggestionType, double confidence);

        #endregion

        #region Analytics Queries

        /// <summary>
        /// Get navigation insights for a time period
        /// </summary>
        NavigationInsights GetInsights(TimeSpan period);

        /// <summary>
        /// Get most common navigation paths
        /// </summary>
        IEnumerable<NavigationPath> GetMostCommonPaths(TimeSpan period, int topN = 10);

        /// <summary>
        /// Get most accessed modules/pages
        /// </summary>
        IEnumerable<PageAccessStats> GetMostAccessedPages(TimeSpan period, int topN = 10);

        /// <summary>
        /// Get average time between navigations
        /// </summary>
        double GetAverageNavigationTime(TimeSpan period);

        /// <summary>
        /// Get navigation error rate
        /// </summary>
        double GetNavigationErrorRate(TimeSpan period);

        /// <summary>
        /// Get user-specific navigation patterns
        /// </summary>
        UserNavigationPattern GetUserPattern(Guid userId, TimeSpan period);

        #endregion

        #region Data Management

        /// <summary>
        /// Clear analytics data older than specified date
        /// </summary>
        void ClearOldData(TimeSpan olderThan);

        /// <summary>
        /// Export analytics data
        /// </summary>
        string ExportData(TimeSpan period, AnalyticsFormat format);

        /// <summary>
        /// Get all raw navigation events
        /// </summary>
        ReadOnlyObservableCollection<NavigationEvent> AllEvents { get; }

        #endregion
    }

    #region Analytics Data Models

    /// <summary>
    /// Navigation event record
    /// </summary>
    public record NavigationEvent(
        DateTime Timestamp,
        string EventType, // "Navigate", "Cancel", "Failure", "History", "Breadcrumb", "Suggestion"
        string FromUri,
        string ToUri,
        Guid? UserId,
        NavigationContext Context,
        Dictionary<string, object>? Metadata = null
    );

    /// <summary>
    /// Navigation insights summary
    /// </summary>
    public record NavigationInsights(
        List<NavigationPath> MostCommonPaths,
        double AverageNavigationTime,
        List<PageAccessStats> MostAccessedPages,
        double ErrorRate,
        DateTime PeriodStart,
        DateTime PeriodEnd,
        int TotalEvents
    );

    /// <summary>
    /// Navigation path (sequence of navigations)
    /// </summary>
    public record NavigationPath(
        string Path, // "/Patient/List → /Patient/Details/123 → /MedicalCase/Create"
        int Frequency,
        double AvgTimeBetweenSteps,
        DateTime LastSeen
    );

    /// <summary>
    /// Page access statistics
    /// </summary>
    public record PageAccessStats(
        string Uri,
        string Title,
        int AccessCount,
        double AvgTimeOnPage,
        DateTime LastAccessed,
        List<string> MostCommonNextPages
    );

    /// <summary>
    /// User navigation pattern
    /// </summary>
    public record UserNavigationPattern(
        Guid UserId,
        List<string> FavoritePages,
        List<NavigationPath> CommonPaths,
        double AvgNavigationTime,
        int TotalNavigations
    );

    /// <summary>
    /// Navigation context for analytics
    /// </summary>
    public record NavigationContext(
        string? SourceModule,
        string? Action,
        Dictionary<string, object>? Parameters
    );

    /// <summary>
    /// Analytics export format
    /// </summary>
    public enum AnalyticsFormat
    {
        Json,
        Csv,
        Xml
    }

    #endregion
}
