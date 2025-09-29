using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")] // 病历依赖患者
    [ModuleDependency("ConsultationModule")] // 病历依赖诊疗
    public class MedicalCaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册简化的服务
            containerRegistry.RegisterSingleton<IMedicalCaseService, MedicalCaseService>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.MedicalCaseManagementViewModel>();
            containerRegistry.Register<ViewModels.MedicalCaseListViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseListView>();
        }
    }
}