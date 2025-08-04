using LYBT.WPF.Client.Modules.Physiotherapy.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Physiotherapy
{
    /// <summary>
    /// 理疗模块
    /// </summary>
    public class PhysiotherapyModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册理疗管理视图
            containerRegistry.RegisterForNavigation<PhysiotherapyManagementView>();
        }
    }
}