using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务接口
    /// 负责管理角色驱动的模块加载
    /// 注意：初始化方法已迁移至IStartupPipeline，保留此接口用于角色模块加载
    /// </summary>
    public interface IApplicationBootstrapper
    {
        /// <summary>
        /// 初始化核心服务
        /// </summary>
        [Obsolete("已迁移至IStartupPipeline，使用CoreServicesStartupStep替代")]
        Task InitializeCoreServicesAsync();

        /// <summary>
        /// 初始化应用程序预热
        /// </summary>
        [Obsolete("已迁移至IStartupPipeline，使用WarmupStartupStep替代")]
        Task InitializeApplicationWarmupAsync();

        /// <summary>
        /// 初始化错误处理服务
        /// </summary>
        [Obsolete("已迁移至IStartupPipeline，使用ErrorHandlingStartupStep替代")]
        void InitializeErrorHandlingService();

        /// <summary>
        /// 初始化简化的模块协调器
        /// </summary>
        [Obsolete("已迁移至IStartupPipeline，使用ModuleCoordinatorStartupStep替代")]
        void InitializeSimplifiedModuleCoordinator();

        /// <summary>
        /// 根据用户角色加载模块
        /// </summary>
        /// <param name="userRole">用户角色</param>
        Task LoadModulesForRoleAsync(UserRole userRole);
    }
}
