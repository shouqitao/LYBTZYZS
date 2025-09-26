using LYBT.Desktop.Herbs.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Herbs
{
    /// <summary>
    /// 药材管理模块 - 简化版
    /// </summary>
    public class HerbsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IHerbService, HerbService>();

            // TODO: 注册简化后的视图和视图模型
        }
    }
}