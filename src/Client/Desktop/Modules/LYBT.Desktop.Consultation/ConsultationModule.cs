using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Consultation
{
    /// <summary>
    /// 诊疗管理模块 - 简化版
    /// Issue #1606 Phase 3: 移除ConsultationRepository/ApiClient（已迁移至MedicalCaseRepository聚合根）
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
            // Issue #1607: ViewModel重构完成，恢复注册
            // 所有Write操作通过MedicalCaseRepository聚合根
            // Read操作使用IConsultationApi

            // Issue #1784: 注册Services（Epic #1773 Component-Based架构）
            // OpenSpec: standardize-module-structure - Components重命名为Services
            containerRegistry.Register<Services.ConsultationDataManager>();
            containerRegistry.Register<Services.ConsultationCommandHandler>();
            containerRegistry.Register<Services.ConsultationValidator>();

            // 注册视图模型 - MVP核心功能
            // Issue #1800: 删除ConsultationManagementViewModel（违反AR-001聚合根约束）
            containerRegistry.Register<ViewModels.ConsultationFormViewModel>();  // Issue #1607, #1784: 已重构使用Components (Issue #1557: 看诊流程Step 2)
            //  Issue #1463: ConsultationEntryViewModel已迁移到MedicalCaseModule.MedicalCaseEntryViewModel

            // Phase 2: 启用 Region Navigation 注册
            // Issue #1800: 删除ConsultationManagementView（违反AR-001聚合根约束）
            containerRegistry.RegisterForNavigation<Views.ConsultationFormView>();  // Issue #1607: 已重构 (Issue #1557: 看诊流程Step 2)
            //  Issue #1463: ConsultationEntryView已迁移到MedicalCaseModule.MedicalCaseEntryView
        }
    }
}
