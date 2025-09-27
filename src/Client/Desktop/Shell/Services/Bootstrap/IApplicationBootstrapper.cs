using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务接口
    /// 负责管理应用程序的初始化流程，避免Service Locator反模式
    /// </summary>
    public interface IApplicationBootstrapper
    {
        /// <summary>
        /// 初始化核心服务
        /// </summary>
        Task InitializeCoreServicesAsync();

        /// <summary>
        /// 初始化应用程序预热
        /// </summary>
        Task InitializeApplicationWarmupAsync();

        /// <summary>
        /// 初始化错误处理服务
        /// </summary>
        void InitializeErrorHandlingService();

        /// <summary>
        /// 初始化简化的模块协调器
        /// </summary>
        void InitializeSimplifiedModuleCoordinator();

        /// <summary>
        /// 根据用户角色加载模块
        /// </summary>
        /// <param name="userRole">用户角色</param>
        Task LoadModulesForRoleAsync(UserRole userRole);
    }
}