using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Herbs.Views;

namespace LYBT.Desktop.Herbs
{
    /// <summary>
    /// 中药材管理模块 - 独立业务模块
    /// 对应后端: LYBT.Module.Herbs
    /// 遵循原始设计：前后端模块一一对应
    /// </summary>
    public class HerbsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<HerbManagementView, HerbManagementViewModelSimple>();
            containerRegistry.RegisterForNavigation<HerbAddEditDialog, HerbAddEditDialogViewModel>();
        }
    }
}