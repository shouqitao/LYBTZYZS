using Prism.Ioc;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Infrastructure.Navigation.Controls;

namespace LYBT.Desktop.Infrastructure.Bootstrapping
{
    /// <summary>
    /// Navigation Service Registration - Phase 2.1: Navigation Improvements
    /// Phase 3: Integration - Service Registration
    /// Phase 4: Analytics & Optimization - Analytics Service Registration
    ///
    /// Usage in application bootstrapper:
    /// containerRegistry.RegisterNavigationServices();
    /// </summary>
    public static class NavigationServiceRegistration
    {
        /// <summary>
        /// Register navigation services with the dependency injection container
        /// </summary>
        public static void RegisterNavigationServices(this IContainerRegistry containerRegistry)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            // Register singleton navigation service
            containerRegistry.RegisterSingleton<IEnhancedNavigationService, EnhancedNavigationService>();

            // Phase 4: Register navigation analytics service
            containerRegistry.RegisterSingleton<INavigationAnalyticsService, NavigationAnalyticsService>();

            // Register optional navigation shortcuts manager
            containerRegistry.RegisterSingleton<NavigationShortcutsManager>();

            // Register navigation UI components (optional - for View injection)
            containerRegistry.Register<BreadcrumbControlViewModel>();
            containerRegistry.Register<NavigationHistoryPanelViewModel>();
            containerRegistry.Register<NavigationSuggestionsPanelViewModel>();
        }

        /// <summary>
        /// Register navigation services with Prism ContainerRegistry
        /// </summary>
        public static void RegisterNavigationServices(this IContainerRegistry containerRegistry, params Type[] navigationTypes)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            // Register the core navigation service
            containerRegistry.RegisterSingleton<IEnhancedNavigationService, EnhancedNavigationService>();

            // Phase 4: Register navigation analytics service
            containerRegistry.RegisterSingleton<INavigationAnalyticsService, NavigationAnalyticsService>();

            // Register any additional navigation types if provided
            foreach (var type in navigationTypes)
            {
                containerRegistry.Register(type);
            }
        }
    }
}
