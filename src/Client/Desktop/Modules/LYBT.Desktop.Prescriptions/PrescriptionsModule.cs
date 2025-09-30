using LYBT.Desktop.Modules.Prescriptions.ViewModels;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(PrescriptionsModule))]
    [ModuleDependency("ConsultationModule")] // 处方依赖诊疗
    [ModuleDependency("HerbsModule")] // 处方依赖药材
    [ModuleDependency("FormulaModule")] // 处方依赖方剂
    public class PrescriptionsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Services由Core_New/Services统一注册，不在Module中注册

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<PrescriptionManagementViewModel>();
            containerRegistry.Register<PrescriptionsMainViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
            // containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();
        }
    }
}
