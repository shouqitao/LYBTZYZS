using LYBT.WPF.Client.BusinessModules.Herbs.Services;
using LYBT.WPF.Client.BusinessModules.Herbs.ViewModels;
using LYBT.WPF.Client.BusinessModules.Herbs.Views;
using LYBT.WPF.Client.BusinessModules.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.BusinessModules.Herbs
{
    /// <summary>
    /// 中药材管理业务模块
    /// </summary>
    public class HerbsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<HerbManagementView, HerbManagementViewModel>();

            // 注册服务
            containerRegistry.RegisterSingleton<ISharedHerbService, SharedHerbService>();

            // 注册对话框
            containerRegistry.RegisterDialog<HerbAddEditDialog, HerbAddEditDialogViewModel>();
        }
    }
}