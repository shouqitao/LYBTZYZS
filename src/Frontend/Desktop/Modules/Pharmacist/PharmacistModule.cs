using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Pharmacist.Views;

namespace LYBT.WPF.Client.Modules.Pharmacist
{
    /// <summary>
    /// 药剂师模块
    /// </summary>
    public class PharmacistModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册药房人员主界面视图
            containerRegistry.RegisterForNavigation<PharmacyStaffMainView>();
        }
    }
}