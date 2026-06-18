using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Herbs.Controls;
using LYBT.Desktop.Herbs.Mappers;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Herbs
{
    /// <summary>
    /// 药材管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(HerbsModule))]
    [ModuleDependency("AuthenticationModule")] // 药材模块只依赖认证
    public class HerbsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ViewModelLocationProvider.Register(typeof(HerbMasterDetailControl).ToString(), typeof(ViewModels.HerbMasterDetailViewModel));

            // IHerbRepository 由 Shell DI 注册 (Refit API)
            containerRegistry.Register<IHerbService, Services.RemoteHerbService>();

            // D5-3: 跨模块药材搜索提供者，供 MedicalCase 模块使用
            containerRegistry.Register<IHerbSearchProvider, Services.HerbSearchProvider>();
            // Issue #2168: CRUD统一架构 - HerbCreateViewModel已删除

            // Handler 组件
            containerRegistry.Register<ViewModels.Handlers.IHerbStatusHandler, ViewModels.Handlers.HerbStatusHandler>();
            containerRegistry.AddMasterDetailServices<HerbListDto, HerbDetailModel>();
            containerRegistry.Register<ViewModels.HerbMasterDetailViewModel>();
            containerRegistry.Register<ViewModels.HerbEditorViewModel>();
            

        }
    }
}
