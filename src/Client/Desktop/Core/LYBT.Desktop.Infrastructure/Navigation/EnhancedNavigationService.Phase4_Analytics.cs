using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// Enhanced Navigation Service - Phase 4: Analytics Integration
    /// Adds analytics tracking to navigation operations
    /// </summary>
    public partial class EnhancedNavigationService
    {
        #region Fields

        private INavigationAnalyticsService? _analyticsService;

        #endregion

        #region Phase 4: Analytics Integration

        /// <summary>
        /// Phase 4: Initialize analytics service (called after construction)
        /// </summary>
        partial void OnAnalyticsInitialized()
        {
            // Analytics service will be injected via property if available
            // This allows optional analytics without breaking existing functionality
            if (_analyticsService != null)
            {
                _logger.LogInformation("导航分析服务已集成");
            }
        }

        /// <summary>
        /// Phase 4: Set analytics service (dependency injection)
        /// </summary>
        public void SetAnalyticsService(INavigationAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _logger.LogInformation("导航分析服务已设置");
        }

        /// <summary>
        /// Phase 4: Track navigation event
        /// </summary>
        private void TrackNavigation(string fromUri, string toUri, NavigationParameters? parameters)
        {
            try
            {
                if (_analyticsService == null) return;

                var context = new NavigationContext(
                    ExtractModuleFromUri(toUri),
                    "Navigation",
                    CreateParametersDictionary(parameters)
                );

                _analyticsService.TrackNavigation(fromUri, toUri, context);
            }
            catch (Exception ex)
            {
                // Don't let analytics errors affect navigation
                _logger.LogWarning(ex, "记录导航分析数据失败");
            }
        }

        /// <summary>
        /// Phase 4: Track navigation cancellation
        /// </summary>
        private void TrackCancellation(string uri, string reason)
        {
            try
            {
                if (_analyticsService == null) return;

                _analyticsService.TrackNavigationCancellation(uri, reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录导航取消失败");
            }
        }

        /// <summary>
        /// Phase 4: Track navigation failure
        /// </summary>
        private void TrackFailure(string uri, string errorMessage)
        {
            try
            {
                if (_analyticsService == null) return;

                _analyticsService.TrackNavigationFailure(uri, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录导航失败失败");
            }
        }

        /// <summary>
        /// Phase 4: Track history navigation (back/forward)
        /// </summary>
        private void TrackHistoryNavigation(bool isBack, string uri)
        {
            try
            {
                if (_analyticsService == null) return;

                _analyticsService.TrackHistoryNavigation(isBack, uri);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录历史导航失败");
            }
        }

        /// <summary>
        /// Phase 4: Track breadcrumb click
        /// </summary>
        private void TrackBreadcrumbClick(string uri, int level)
        {
            try
            {
                if (_analyticsService == null) return;

                _analyticsService.TrackBreadcrumbClick(uri, level);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录面包屑点击失败");
            }
        }

        /// <summary>
        /// Phase 4: Track suggestion usage
        /// </summary>
        private void TrackSuggestionUsage(string uri, SuggestionType suggestionType, double confidence)
        {
            try
            {
                if (_analyticsService == null) return;

                _analyticsService.TrackSuggestionUsage(uri, suggestionType, confidence);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录导航建议使用失败");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Extract module name from URI
        /// </summary>
        private string? ExtractModuleFromUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            var parts = uri.Trim('/').Split('/');
            return parts.Length > 0 ? parts[0] : null;
        }

        /// <summary>
        /// Convert NavigationParameters to dictionary
        /// </summary>
        private Dictionary<string, object>? CreateParametersDictionary(NavigationParameters? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            var dict = new Dictionary<string, object>();
            foreach (var key in parameters.Keys)
            {
                dict[key] = parameters[key];
            }
            return dict;
        }

        #endregion
    }
}
