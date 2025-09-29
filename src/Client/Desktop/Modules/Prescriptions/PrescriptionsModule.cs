using LYBT.Desktop.Prescriptions.Services;
using LYBT.Shared.Interfaces.Services;
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
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IPrescriptionService, PrescriptionService>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.PrescriptionManagementViewModel>();
            containerRegistry.Register<ViewModels.PrescriptionsMainViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.PrescriptionManagementView>();
            // containerRegistry.RegisterForNavigation<Views.PrescriptionsMainView>();
        }
    }
}