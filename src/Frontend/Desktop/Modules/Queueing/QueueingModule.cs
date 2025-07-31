using LYBT.WPF.Client.Modules.Queueing.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Queueing
{
    /// <summary>
    /// 排队管理模块
    /// </summary>
    public class QueueingModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册排队管理主视图
            containerRegistry.RegisterForNavigation<QueueingMainView>();
        }
    }
}