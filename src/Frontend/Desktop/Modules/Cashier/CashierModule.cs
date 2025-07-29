using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Cashier.Views;

namespace LYBT.WPF.Client.Modules.Cashier
{
    /// <summary>
    /// 收银员模块
    /// </summary>
    public class CashierModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册收银员主界面视图
            containerRegistry.RegisterForNavigation<CashierMainView>();
        }
    }
}