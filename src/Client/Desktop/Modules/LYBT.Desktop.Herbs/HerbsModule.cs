using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Herbs.Mappers;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Herbs.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Ioc;
using Prism.Modularity;

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
            // ADR-002 架构标准：
            // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
            // - Repository (数据访问层) 由各业务模块自行注册
            containerRegistry.RegisterSingleton<IHerbRepository, HerbRepository>();

            // OpenSpec: adopt-mapperly-unified-mapping - 注册映射服务
            containerRegistry.RegisterSingleton<
                IMappingService<HerbDetailDto, HerbInputDto, HerbDetailModel>,
                HerbMappingService>();

            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.RegisterScoped<IHerbService, HerbService>();

            // OpenSpec: migrate-views-to-role-modules - HerbDetailView/HerbDetailViewModel已删除（无调用）
            // Issue #2168: CRUD统一架构 - HerbCreateViewModel已删除

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Herbs模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<HerbListDto, HerbDetailModel>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // HerbMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.HerbMasterDetailViewModel>();
        }
    }
}
