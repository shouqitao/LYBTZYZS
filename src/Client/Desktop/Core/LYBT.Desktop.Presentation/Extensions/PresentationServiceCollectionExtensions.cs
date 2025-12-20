using LYBT.Desktop.Presentation.Notifications;
using LYBT.Desktop.Presentation.UserExperience;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Presentation.Extensions
{
    /// <summary>
    /// Desktop Presentation 服务注册扩展方法
    /// Issue #1114 Phase 1.5 - UI基础设施层服务注册
    /// optimize-desktop-core: 使用IDesktopExceptionHandler统一异常处理
    /// </summary>
    public static class PresentationServiceCollectionExtensions
    {
        /// <summary>
        /// 注册Desktop Presentation层服务（UI基础设施）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDesktopPresentation(this IServiceCollection services)
        {
            // 通知服务
            services.AddSingleton<INotificationService, NotificationService>();

            // 异常处理服务 - optimize-desktop-core: 统一使用Shared.ExceptionHandling
            services.AddSingleton<IDesktopExceptionHandler, DesktopExceptionHandler>();

            // 用户体验服务
            services.AddSingleton<IUserExperienceService, UserExperienceService>();

            // 注意：
            // - INavigationService 需要Prism实现，暂时不注册
            // - IPrescriptionPrintService 需要具体实现，暂时不注册

            return services;
        }
    }
}
