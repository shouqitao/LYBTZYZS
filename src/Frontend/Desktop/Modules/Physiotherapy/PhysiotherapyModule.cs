using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Physiotherapy.Views;

namespace LYBT.WPF.Client.Modules.Physiotherapy
{
    /// <summary>
    /// 理疗师模块
    /// </summary>
    public class PhysiotherapyModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册理疗师主界面视图
            containerRegistry.RegisterForNavigation<PhysiotherapyStaffMainView>();
        }
    }
}