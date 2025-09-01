
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Formula.Views;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.Services;
using LYBT.Shared.Interfaces.Services;

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
            // UltraThink模块自治：注册业务服务接口实现
            containerRegistry.RegisterSingleton<Services.FormulaModule>();
            containerRegistry.RegisterSingleton<IFormulaService>(container => container.Resolve<Services.FormulaModule>());
            
            // UltraThink四层架构：注册标准ViewModel
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