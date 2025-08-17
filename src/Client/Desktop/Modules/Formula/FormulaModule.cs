
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Formula.Views;
using LYBT.Desktop.Formula.ViewModels;

namespace LYBT.Desktop.Formula
{
    /// <summary>
    /// 验方管理模块
    /// </summary>
    public class FormulaModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化逻辑
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink Phase 3.4: 注册增强版验方管理ViewModel
            containerRegistry.RegisterForNavigation<FormulaManagementView, FormulaManagementViewModelEnhanced>();
            
            // 注册对话框
            containerRegistry.RegisterForNavigation<AddFormulaDialog, AddFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<EditFormulaDialog, EditFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<ViewFormulaDialog, ViewFormulaDialogViewModel>();
        }
    }
}