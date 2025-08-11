using LYBT.Desktop.Herbs.Shared.Services;
using LYBT.Desktop.Herbs.Shared.ViewModels;
using LYBT.Desktop.Herbs.Shared.Views;
using LYBT.Desktop.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Herbs.Shared
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