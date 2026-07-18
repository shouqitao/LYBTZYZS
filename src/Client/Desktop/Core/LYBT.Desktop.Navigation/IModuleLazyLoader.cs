using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 业务模块懒加载接口
/// </summary>
public interface IModuleLazyLoader
{
    /// <summary>确保目标视图所属的业务模块已加载（同步，内部用 Task.Run 不会死锁）</summary>
    void EnsureModuleLoaded(string viewName);

    /// <summary>确保目标视图所属的业务模块已加载（异步版本）</summary>
    Task EnsureModuleLoadedAsync(string viewName);

    /// <summary>
    /// 预加载指定角色的高频模块
    /// </summary>
    Task PreloadModulesAsync(UserRole role);
}
