using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Consultation
{
    /// <summary>
    /// 诊疗管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(ConsultationModule))]
    [ModuleDependency("PatientsModule")] // 诊疗依赖患者
    public class ConsultationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Services由Core_New/Services统一注册，不在Module中注册

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.ConsultationMainViewModel>();
            containerRegistry.Register<ViewModels.ConsultationManagementViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.ConsultationMainView>();
            // containerRegistry.RegisterForNavigation<Views.ConsultationManagementView>();
        }
    }
}