using LYBT.WPF.Client.Modules.Registration.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Registration
{
    /// <summary>
    /// 挂号管理模块
    /// </summary>
    public class RegistrationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册挂号管理主视图
            containerRegistry.RegisterForNavigation<RegistrationMainView>();
        }
    }
}