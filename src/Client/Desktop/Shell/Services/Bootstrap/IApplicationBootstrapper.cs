using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务接口
    /// 职责：角色驱动的模块加载
    /// 注意：初始化逻辑已迁移至IStartupPipeline和各StartupStep
    /// </summary>
    public interface IApplicationBootstrapper
    {
        /// <summary>
        /// 根据用户角色加载模块
        /// </summary>
        /// <param name="userRole">用户角色</param>
        Task LoadModulesForRoleAsync(UserRole userRole);
    }
}
