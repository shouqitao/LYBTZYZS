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
            // Services由Core_New/Services统一注册，不在Module中注册

            // Phase 3.4: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();

            // 注册视图模型 - MVP核心功能
            // TODO: 修复编译错误后再启用
            // containerRegistry.Register<MedicalCaseManagementViewModel>();
            // containerRegistry.Register<MedicalCaseListViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseListView>();
        }
    }
}
