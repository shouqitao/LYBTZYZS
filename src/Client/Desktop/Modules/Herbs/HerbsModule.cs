using LYBT.Desktop.Herbs.Services;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Herbs.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Herbs
{

    /// <summary>
    /// 中药材管理模块 - UltraThink双层架构Prism模块
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：中药材管理模块依赖注入、服务注册、视图导航配置
    /// 实现中药材档案管理、用法用量、价格管理、Excel导入导出等功能
    /// 集成双层架构服务（QueryService + BusinessService + Module委托）
    /// 适配中医诊所药材管理流程，确保药材信息准确和处方选择便利性
    /// </summary>
    public class HerbsModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Herbs.Interfaces.IHerbQueryService, HerbQueryService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Herbs.Interfaces.IHerbBusinessService, HerbBusinessService>();

            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.HerbModule>();
            containerRegistry.RegisterSingleton<IHerbService>(container => container.Resolve<Services.HerbModule>());

            // 视图和视图模型注册
            containerRegistry.RegisterForNavigation<HerbManagementView, HerbManagementViewModel>();
            containerRegistry.RegisterForNavigation<HerbAddEditDialog, HerbAddEditDialogViewModel>();
            containerRegistry.RegisterForNavigation<HerbDetailView, HerbDetailViewModel>();
        }
    }
}
