using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Prescriptions.ViewModels;
using LYBT.Desktop.Prescriptions.Views;
using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Prescriptions.Components;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方管理模块 - 核心业务模块
    /// 对应后端: LYBT.Module.Prescriptions
    /// 功能：处方开具、编辑、打印、历史查询
    /// </summary>
    public class PrescriptionsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink简化架构：注册核心组件和服务
            
            // 核心组件（简化版）
            containerRegistry.RegisterSingleton<PriceCalculator>();
            containerRegistry.RegisterSingleton<BasicValidator>();
            
            // 简化服务
            containerRegistry.RegisterSingleton<IPrescriptionComposerService, PrescriptionComposerService>();
            
            // UltraThink模块自治：注册业务服务接口实现
            containerRegistry.RegisterSingleton<PrescriptionsModule>();
            containerRegistry.RegisterSingleton<IPrescriptionService>(container => container.Resolve<PrescriptionsModule>());
            
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