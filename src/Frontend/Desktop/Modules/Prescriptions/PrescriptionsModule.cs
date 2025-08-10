using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Prescriptions.ViewModels;
using LYBT.WPF.Client.Modules.Prescriptions.Views;

namespace LYBT.WPF.Client.Modules.Prescriptions
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
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<PrescriptionManagementView, PrescriptionManagementViewModel>();
            containerRegistry.RegisterForNavigation<PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();
            containerRegistry.RegisterForNavigation<HerbSelectionDialog, HerbSelectionDialogViewModel>();
            containerRegistry.RegisterForNavigation<FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
        }
    }
}