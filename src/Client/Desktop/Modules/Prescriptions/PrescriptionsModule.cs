using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Prescriptions.ViewModels;
using LYBT.Desktop.Prescriptions.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions
{

    /// <summary>
    /// 处方管理模块 - UltraThink双层架构Prism模块
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：处方管理模块依赖注入、服务注册、视图导航配置
    /// 实现中医处方开具、药材配伍、验方组合、智能计算等核心功能
    /// 集成双层架构服务（QueryService + BusinessService + Module委托）
    /// 适配中医诊所处方管理流程，确保配伍安全和计算准确性
    /// </summary>
    public class PrescriptionsModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Prescriptions.Interfaces.IPrescriptionsQueryService, PrescriptionsQueryService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Prescriptions.Interfaces.IPrescriptionsBusinessService, PrescriptionsBusinessService>();

            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.PrescriptionsModule>();
            containerRegistry.RegisterSingleton<IPrescriptionService>(container => container.Resolve<Services.PrescriptionsModule>());

            // UltraThink核心视图：专注处方组成编辑

            // 主视图：处方组成编辑器（简化版核心）
            containerRegistry.RegisterForNavigation<PrescriptionComposerView, PrescriptionComposerViewModel>();

            // 主入口视图（兼容性保持）
            containerRegistry.RegisterForNavigation<PrescriptionsMainView, PrescriptionsMainViewModel>();

            // 保留的处方工作流视图（向后兼容）
            // containerRegistry.RegisterForNavigation<PrescriptionView, PrescriptionViewModel>(); // Temporarily disabled - legacy code
            containerRegistry.RegisterForNavigation<PrescriptionManagementView, PrescriptionManagementViewModel>();

            // 对话框视图（向后兼容）
            containerRegistry.RegisterForNavigation<PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();
            containerRegistry.RegisterForNavigation<HerbSelectionDialog, HerbSelectionDialogViewModel>();
            containerRegistry.RegisterForNavigation<FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
            containerRegistry.RegisterForNavigation<SelectFormulaDialog, SelectFormulaDialogViewModel>();
        }
    }
}
