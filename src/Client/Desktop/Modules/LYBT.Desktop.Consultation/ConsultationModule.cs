using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Consultation.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Consultation
{
    /// <summary>
    /// 诊疗管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(ConsultationModule))]
    [ModuleDependency("PatientsModule")] // 诊疗依赖患者
    [ModuleDependency("MedicalCaseModule")] // 诊疗依赖医疗案例 (Issue #1459修复)
    public class ConsultationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Phase 3: 注册 Repository（模块级数据访问）
            containerRegistry.RegisterSingleton<IConsultationRepository, ConsultationRepository>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.ConsultationManagementViewModel>();
            containerRegistry.Register<ViewModels.ConsultationFormViewModel>();  // Issue #1557: 看诊流程Step 2
            // ✅ Issue #1463: ConsultationEntryViewModel已迁移到MedicalCaseModule.MedicalCaseEntryViewModel

            // Phase 2: 启用 Region Navigation 注册
            containerRegistry.RegisterForNavigation<Views.ConsultationManagementView>();
            containerRegistry.RegisterForNavigation<Views.ConsultationFormView>();  // Issue #1557: 看诊流程Step 2（Region导航）
            // ✅ Issue #1463: ConsultationEntryView已迁移到MedicalCaseModule.MedicalCaseEntryView
        }
    }
}
