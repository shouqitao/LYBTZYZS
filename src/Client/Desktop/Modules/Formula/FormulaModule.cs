using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Formula {

    /// <summary>
    /// 验方管理模块 - UltraThink双层架构Prism模块
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：验方管理模块依赖注入、服务注册、视图导航配置
    /// 实现经典验方库管理、个人验方创建、验方组合、处方引用等功能
    /// 集成双层架构服务（QueryService + BusinessService + Module委托）
    /// 适配中医诊所验方管理流程，确保验方质量和临床应用便利性
    /// </summary>
    public class FormulaModule : IModule {

        public void OnInitialized(IContainerProvider containerProvider) {
            // 模块初始化逻辑
        }

        public void RegisterTypes(IContainerRegistry containerRegistry) {
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<IFormulaQueryService, FormulaQueryService>();
            containerRegistry.RegisterSingleton<IFormulaBusinessService, FormulaBusinessService>();

            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.FormulaModule>();
            containerRegistry.RegisterSingleton<IFormulaService>(container => container.Resolve<Services.FormulaModule>());

            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<FormulaManagementView, FormulaManagementViewModel>();

            // 注册对话框
            containerRegistry.RegisterForNavigation<AddFormulaDialog, AddFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<EditFormulaDialog, EditFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<ViewFormulaDialog, ViewFormulaDialogViewModel>();

            // 注册详情视图
            containerRegistry.RegisterForNavigation<FormulaDetailView, FormulaDetailViewModel>();
        }
    }
}
