using LYBT.WPF.Client.BusinessModules.Formula.Services;
using LYBT.WPF.Client.BusinessModules.Formula.ViewModels;
using LYBT.WPF.Client.BusinessModules.Formula.Views;
using LYBT.WPF.Client.BusinessModules.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.BusinessModules.Formula
{
    /// <summary>
    /// 验方管理业务模块
    /// </summary>
    public class FormulaModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<FormulaManagementView, FormulaManagementViewModel>();

            // 注册服务
            containerRegistry.RegisterSingleton<ISharedFormulaService, SharedFormulaService>();

            // 注册对话框
            containerRegistry.RegisterDialog<FormulaAddEditDialog, FormulaAddEditDialogViewModel>();
        }
    }
}