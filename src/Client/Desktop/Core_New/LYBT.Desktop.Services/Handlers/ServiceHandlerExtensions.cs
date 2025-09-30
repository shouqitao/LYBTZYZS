using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.Services.Handlers
{
    /// <summary>
    /// 服务处理程序扩展类
    /// </summary>
    public static class ServiceHandlerExtensions
    {
        /// <summary>
        /// 添加服务处理程序
        /// </summary>
        public static IServiceCollection AddServiceHandlers(this IServiceCollection services)
        {
            // 服务处理程序配置逻辑
            return services;
        }
    }
}
