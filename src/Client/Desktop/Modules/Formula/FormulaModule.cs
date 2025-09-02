
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Formula.Views;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Formula.Interfaces;
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
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<IFormulaQueryService, FormulaQueryService>();
            containerRegistry.RegisterSingleton<IFormulaBusinessService, FormulaBusinessService>();
            
            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.FormulaModule>();
            containerRegistry.RegisterSingleton<IFormulaService>(container => container.Resolve<Services.FormulaModule>());
            containerRegistry.RegisterSingleton<IFormulaModule>(container => container.Resolve<Services.FormulaModule>());
            
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