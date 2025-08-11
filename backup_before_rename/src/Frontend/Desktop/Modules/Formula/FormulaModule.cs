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
            // 注册视图导航
            containerRegistry.RegisterForNavigation<FormulaManagementView, FormulaManagementViewModel>();
            
            // 注册对话框
            containerRegistry.RegisterForNavigation<AddFormulaDialog, AddFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<EditFormulaDialog, EditFormulaDialogViewModel>();
            containerRegistry.RegisterForNavigation<ViewFormulaDialog, ViewFormulaDialogViewModel>();
        }
    }
}