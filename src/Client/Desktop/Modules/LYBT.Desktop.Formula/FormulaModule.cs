using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Mappers;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.Models.Items;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Shared.Models.Contracts.Formula;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Formula
{
    /// <summary>
    /// 验方管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(FormulaModule))]
    [ModuleDependency("HerbsModule")] // 方剂依赖药材
    public class FormulaModule : IModule
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
            containerRegistry.RegisterSingleton<IFormulaRepository, FormulaRepository>();

            // OpenSpec: adopt-mapperly-unified-mapping - 注册映射服务
            containerRegistry.RegisterSingleton<
                IMappingService<FormulaDetailDto, FormulaInputDto, FormulaItem>,
                FormulaMappingService>();
            containerRegistry.RegisterSingleton<
                IMappingService<FormulaDetailDto, FormulaInputDto, FormulaDetailModel>,
                FormulaDetailModelMappingService>();

            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<IFormulaService, Services.FormulaService>();
            containerRegistry.Register<Services.FormulaValidator>();

            // OpenSpec: migrate-views-to-role-modules - FormulaDetailView/FormulaValidationView已删除（无调用）
            // OpenSpec: migrate-views-to-role-modules - EditFormulaDialog已删除（无调用）
            // OpenSpec: migrate-views-to-role-modules - FormulaDetailViewModel/FormulaValidationViewModel已删除（无调用）

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Formula模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<FormulaListDto, FormulaDetailModel>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // FormulaMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.FormulaMasterDetailViewModel>();
        }
    }
}
