using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// Navigation Analytics Service Implementation - Phase 4: Analytics & Optimization
    /// Track and analyze navigation patterns for continuous improvement
    /// </summary>
    public class NavigationAnalyticsService : INavigationAnalyticsService
    {
        #region Fields

        private readonly ILogger<NavigationAnalyticsService> _logger;
        private readonly ObservableCollection<NavigationEvent> _events;
        private readonly ReadOnlyObservableCollection<NavigationEvent> _readonlyEvents;
        private readonly object _lock = new();

        // Performance tracking
        private DateTime? _lastNavigationTime;
        private readonly List<double> _navigationTimes = new();

        #endregion

        #region Constructor

        public NavigationAnalyticsService(ILogger<NavigationAnalyticsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _events = new ObservableCollection<NavigationEvent>();
            _readonlyEvents = new ReadOnlyObservableCollection<NavigationEvent>(_events);

            _logger.LogInformation("导航分析服务已初始化");
        }

        #endregion

        #region Tracking Methods

        /// <summary>
        /// Track a navigation event
        /// </summary>
        public void TrackNavigation(string fromUri, string toUri, NavigationContext context)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Track navigation time
                if (_lastNavigationTime.HasValue)
                {
                    var navTime = (now - _lastNavigationTime.Value).TotalMilliseconds;
                    _navigationTimes.Add(navTime);

                    // Keep only last 1000 measurements
                    if (_navigationTimes.Count > 1000)
                    {
                        _navigationTimes.RemoveAt(0);
                    }
                }
                _lastNavigationTime = now;

                // Create navigation event
                var navEvent = new NavigationEvent(
                    now,
                    "Navigate",
                    fromUri,
                    toUri,
                    GetCurrentUserId(),
                    context,
                    new Dictionary<string, object>
                    {
                        { "TimeBetweenNavigations", _navigationTimes.Count > 0 ? _navigationTimes[^1] : 0 }
                    }
                );

                AddEvent(navEvent);

                _logger.LogDebug("导航事件已记录: {FromUri} → {ToUri}", fromUri, toUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录导航事件失败");
            }
        }

        /// <summary>
        /// Track navigation cancellation
        /// </summary>
        public void TrackNavigationCancellation(string uri, string reason)
        {
            try
            {
                var navEvent = new NavigationEvent(
                    DateTime.UtcNow,
                    "Cancel",
                    uri,
                    string.Empty,
                    GetCurrentUserId(),
                    new NavigationContext(null, "Cancellation", new Dictionary<string, object> { { "Reason", reason } }),
                    new Dictionary<string, object> { { "Reason", reason } }
                );

                AddEvent(navEvent);

                _logger.LogDebug("导航取消事件已记录: {Uri}, 原因: {Reason}", uri, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录导航取消事件失败");
            }
        }

        /// <summary>
        /// Track navigation failure
        /// </summary>
        public void TrackNavigationFailure(string uri, string errorMessage)
        {
            try
            {
                var navEvent = new NavigationEvent(
                    DateTime.UtcNow,
                    "Failure",
                    uri,
                    string.Empty,
                    GetCurrentUserId(),
                    new NavigationContext(null, "Failure", new Dictionary<string, object> { { "Error", errorMessage } }),
                    new Dictionary<string, object> { { "ErrorMessage", errorMessage } }
                );

                AddEvent(navEvent);

                _logger.LogWarning("导航失败事件已记录: {Uri}, 错误: {Error}", uri, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录导航失败事件失败");
            }
        }

        /// <summary>
        /// Track back/forward navigation
        /// </summary>
        public void TrackHistoryNavigation(bool isBack, string uri)
        {
            try
            {
                var navEvent = new NavigationEvent(
                    DateTime.UtcNow,
                    "History",
                    isBack ? "[Back]" : "[Forward]",
                    uri,
                    GetCurrentUserId(),
                    new NavigationContext(null, isBack ? "BackNavigation" : "ForwardNavigation", null),
                    new Dictionary<string, object> { { "Direction", isBack ? "Back" : "Forward" } }
                );

                AddEvent(navEvent);

                _logger.LogDebug("历史导航事件已记录: {Direction} → {Uri}", isBack ? "返回" : "前进", uri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录历史导航事件失败");
            }
        }

        /// <summary>
        /// Track breadcrumb click
        /// </summary>
        public void TrackBreadcrumbClick(string uri, int level)
        {
            try
            {
                var navEvent = new NavigationEvent(
                    DateTime.UtcNow,
                    "Breadcrumb",
                    $"[Level {level}]",
                    uri,
                    GetCurrentUserId(),
                    new NavigationContext(null, "BreadcrumbClick", null),
                    new Dictionary<string, object> { { "BreadcrumbLevel", level } }
                );

                AddEvent(navEvent);

                _logger.LogDebug("面包屑导航事件已记录: 级别 {Level} → {Uri}", level, uri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录面包屑导航事件失败");
            }
        }

        /// <summary>
        /// Track suggestion usage
        /// </summary>
        public void TrackSuggestionUsage(string uri, SuggestionType suggestionType, double confidence)
        {
            try
            {
                var navEvent = new NavigationEvent(
                    DateTime.UtcNow,
                    "Suggestion",
                    $"[{suggestionType}]",
                    uri,
                    GetCurrentUserId(),
                    new NavigationContext(null, "SuggestionUsed", null),
                    new Dictionary<string, object>
                    {
                        { "SuggestionType", suggestionType.ToString() },
                        { "Confidence", confidence }
                    }
                );

                AddEvent(navEvent);

                _logger.LogDebug("导航建议使用已记录: {Type}, 置信度: {Confidence} → {Uri}",
                    suggestionType, confidence, uri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录导航建议使用失败");
            }
        }

        #endregion

        #region Analytics Queries

        /// <summary>
        /// Get navigation insights for a time period
        /// </summary>
        public NavigationInsights GetInsights(TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var relevantEvents = _events.Where(e => e.Timestamp >= startTime).ToList();

            var mostCommonPaths = GetMostCommonPaths(period, 10);
            var avgNavTime = GetAverageNavigationTime(period);
            var mostAccessed = GetMostAccessedPages(period, 10);
            var errorRate = GetNavigationErrorRate(period);

            return new NavigationInsights(
                mostCommonPaths.ToList(),
                avgNavTime,
                mostAccessed.ToList(),
                errorRate,
                startTime,
                now,
                relevantEvents.Count
            );
        }

        /// <summary>
        /// Get most common navigation paths
        /// </summary>
        public IEnumerable<NavigationPath> GetMostCommonPaths(TimeSpan period, int topN = 10)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            // Group navigation events by paths (sequences of 2-3 navigations)
            var pathGroups = new Dictionary<string, List<double>>();

            for (int i = 0; i < _events.Count - 1; i++)
            {
                if (_events[i].Timestamp < startTime) continue;

                // Create 2-step path
                if (_events[i].EventType == "Navigate" && i + 1 < _events.Count &&
                    _events[i + 1].EventType == "Navigate")
                {
                    var path = $"{_events[i].ToUri} → {_events[i + 1].ToUri}";

                    if (!pathGroups.ContainsKey(path))
                    {
                        pathGroups[path] = new List<double>();
                    }

                    var timeBetween = (_events[i + 1].Timestamp - _events[i].Timestamp).TotalMilliseconds;
                    pathGroups[path].Add(timeBetween);
                }
            }

            // Convert to NavigationPath records
            return pathGroups
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(topN)
                .Select(kvp => new NavigationPath(
                    kvp.Key,
                    kvp.Value.Count,
                    kvp.Value.Count > 0 ? kvp.Value.Average() : 0,
                    now
                ));
        }

        /// <summary>
        /// Get most accessed modules/pages
        /// </summary>
        public IEnumerable<PageAccessStats> GetMostAccessedPages(TimeSpan period, int topN = 10)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            // Group by destination URI
            var pageGroups = _events
                .Where(e => e.EventType == "Navigate" && e.Timestamp >= startTime)
                .GroupBy(e => e.ToUri)
                .Select(g => new
                {
                    Uri = g.Key,
                    AccessCount = g.Count(),
                    LastAccessed = g.Max(e => e.Timestamp),
                    AvgTimeOnPage = CalculateAverageTimeOnPage(g.ToList())
                })
                .OrderByDescending(p => p.AccessCount)
                .Take(topN)
                .Select(p => new PageAccessStats(
                    p.Uri,
                    ExtractTitleFromUri(p.Uri),
                    p.AccessCount,
                    p.AvgTimeOnPage,
                    p.LastAccessed,
                    GetCommonNextPages(p.Uri, period, 5).ToList()
                ));

            return pageGroups;
        }

        /// <summary>
        /// Get average time between navigations
        /// </summary>
        public double GetAverageNavigationTime(TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var relevantTimes = _navigationTimes;

            if (relevantTimes.Count == 0)
                return 0;

            return relevantTimes.Average();
        }

        /// <summary>
        /// Get navigation error rate
        /// </summary>
        public double GetNavigationErrorRate(TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var relevantEvents = _events.Where(e => e.Timestamp >= startTime).ToList();

            if (relevantEvents.Count == 0)
                return 0;

            var errorEvents = relevantEvents.Count(e =>
                e.EventType == "Cancel" || e.EventType == "Failure");

            return (double)errorEvents / relevantEvents.Count * 100;
        }

        /// <summary>
        /// Get user-specific navigation patterns
        /// </summary>
        public UserNavigationPattern GetUserPattern(Guid userId, TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var userEvents = _events
                .Where(e => e.UserId == userId && e.Timestamp >= startTime)
                .ToList();

            if (userEvents.Count == 0)
            {
                return new UserNavigationPattern(
                    userId,
                    new List<string>(),
                    new List<NavigationPath>(),
                    0,
                    0
                );
            }

            // Favorite pages (most accessed)
            var favoritePages = userEvents
                .Where(e => e.EventType == "Navigate")
                .GroupBy(e => e.ToUri)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            // Common paths
            var commonPaths = GetMostCommonPaths(period, 5).Where(p =>
                userEvents.Any(e => e.ToUri == p.Path.Split(" → ")[0])
            ).ToList();

            // Average navigation time
            var avgTime = userEvents
                .Where(e => e.Metadata != null && e.Metadata.ContainsKey("TimeBetweenNavigations"))
                .Select(e => (double)e.Metadata!["TimeBetweenNavigations"])
                .DefaultIfEmpty(0)
                .Average();

            return new UserNavigationPattern(
                userId,
                favoritePages,
                commonPaths,
                avgTime,
                userEvents.Count
            );
        }

        #endregion

        #region Data Management

        /// <summary>
        /// Clear analytics data older than specified time
        /// </summary>
        public void ClearOldData(TimeSpan olderThan)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.Subtract(olderThan);

                lock (_lock)
                {
                    var eventsToRemove = _events.Where(e => e.Timestamp < cutoffDate).ToList();

                    foreach (var evt in eventsToRemove)
                    {
                        _events.Remove(evt);
                    }

                    _logger.LogInformation("已清除 {Count} 条过期导航数据 (早于 {Date})",
                        eventsToRemove.Count, cutoffDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除过期导航数据失败");
            }
        }

        /// <summary>
        /// Export analytics data
        /// </summary>
        public string ExportData(TimeSpan period, AnalyticsFormat format)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var relevantEvents = _events.Where(e => e.Timestamp >= startTime).ToList();

            return format switch
            {
                AnalyticsFormat.Json => ExportAsJson(relevantEvents),
                AnalyticsFormat.Csv => ExportAsCsv(relevantEvents),
                AnalyticsFormat.Xml => ExportAsXml(relevantEvents),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }

        /// <summary>
        /// Get all raw navigation events
        /// </summary>
        public ReadOnlyObservableCollection<NavigationEvent> AllEvents => _readonlyEvents;

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Add event to collection (thread-safe)
        /// </summary>
        private void AddEvent(NavigationEvent navEvent)
        {
            lock (_lock)
            {
                _events.Add(navEvent);

                // Keep collection size manageable (max 10,000 events)
                if (_events.Count > 10000)
                {
                    _events.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Get current user ID (placeholder)
        /// TODO: Integrate with authentication service
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            // TODO: Get from IAuthenticationService when available
            return Guid.Empty; // Anonymous for now
        }

        /// <summary>
        /// Extract title from URI
        /// </summary>
        private string ExtractTitleFromUri(string uri)
        {
            // Simple URI to title conversion
            var parts = uri.Trim('/').Split('/');
            return parts.Length > 0 ? parts[^1] : uri;
        }

        /// <summary>
        /// Calculate average time on page
        /// </summary>
        private double CalculateAverageTimeOnPage(List<NavigationEvent> pageEvents)
        {
            if (pageEvents.Count < 2)
                return 0;

            var times = new List<double>();

            for (int i = 0; i < pageEvents.Count - 1; i++)
            {
                var timeOnPage = (pageEvents[i + 1].Timestamp - pageEvents[i].Timestamp).TotalMilliseconds;
                times.Add(timeOnPage);
            }

            return times.Count > 0 ? times.Average() : 0;
        }

        /// <summary>
        /// Get common next pages after a specific URI
        /// </summary>
        private IEnumerable<string> GetCommonNextPages(string uri, TimeSpan period, int topN)
        {
            var now = DateTime.UtcNow;
            var startTime = now.Subtract(period);

            var nextPages = new List<string>();

            for (int i = 0; i < _events.Count - 1; i++)
            {
                if (_events[i].ToUri == uri && _events[i].Timestamp >= startTime &&
                    _events[i + 1].EventType == "Navigate")
                {
                    nextPages.Add(_events[i + 1].ToUri);
                }
            }

            return nextPages
                .GroupBy(p => p)
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .Select(g => g.Key);
        }

        /// <summary>
        /// Export data as JSON
        /// </summary>
        private string ExportAsJson(List<NavigationEvent> events)
        {
            return JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Export data as CSV
        /// </summary>
        private string ExportAsCsv(List<NavigationEvent> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,EventType,FromUri,ToUri,UserId,Context");

            foreach (var evt in events)
            {
                sb.AppendLine($"{evt.Timestamp:O},{evt.EventType},{evt.FromUri},{evt.ToUri},{evt.UserId},{evt.Context.Action}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export data as XML
        /// </summary>
        private string ExportAsXml(List<NavigationEvent> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<NavigationEvents>");

            foreach (var evt in events)
            {
                sb.AppendLine($"  <Event>");
                sb.AppendLine($"    <Timestamp>{evt.Timestamp:O}</Timestamp>");
                sb.AppendLine($"    <EventType>{evt.EventType}</EventType>");
                sb.AppendLine($"    <FromUri>{evt.FromUri}</FromUri>");
                sb.AppendLine($"    <ToUri>{evt.ToUri}</ToUri>");
                sb.AppendLine($"    <UserId>{evt.UserId}</UserId>");
                sb.AppendLine($"  </Event>");
            }

            sb.AppendLine("</NavigationEvents>");
            return sb.ToString();
        }

        #endregion
    }
}
