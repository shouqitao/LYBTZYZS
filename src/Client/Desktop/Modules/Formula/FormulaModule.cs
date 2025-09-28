using LYBT.Desktop.Formula.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Formula
{
    /// <summary>
    /// 验方管理模块 - 简化版
    /// </summary>
    public class FormulaModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IFormulaService, FormulaService>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.FormulaManagementViewModel>();
            containerRegistry.Register<ViewModels.FormulaDetailViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
            // containerRegistry.RegisterForNavigation<Views.FormulaDetailView>();
        }
    }
}