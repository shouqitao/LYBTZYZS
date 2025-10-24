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
            // Issue #1606 Phase 3: Repository已删除，所有Write操作通过MedicalCaseRepository聚合根
            // 原：IConsultationRepository → 已删除
            // 原：IConsultationApiClient → 已删除

            // ⚠️ Issue #1606 Phase 3: 临时注释，待Issue #1607重构这两个ViewModel
            // 原因：依赖已删除的IConsultationRepository/IConsultationApiClient
            // 注册视图模型 - MVP核心功能
            // containerRegistry.Register<ViewModels.ConsultationManagementViewModel>();  // 待重构 Issue #1607
            // containerRegistry.Register<ViewModels.ConsultationFormViewModel>();  // 待重构 Issue #1607 (Issue #1557: 看诊流程Step 2)
            // ✅ Issue #1463: ConsultationEntryViewModel已迁移到MedicalCaseModule.MedicalCaseEntryViewModel

            // Phase 2: 启用 Region Navigation 注册
            // ⚠️ Issue #1606 Phase 3: 临时注释，待Issue #1607重构这两个ViewModel
            // containerRegistry.RegisterForNavigation<Views.ConsultationManagementView>();  // 待重构 Issue #1607
            // containerRegistry.RegisterForNavigation<Views.ConsultationFormView>();  // 待重构 Issue #1607 (Issue #1557: 看诊流程Step 2)
            // ✅ Issue #1463: ConsultationEntryView已迁移到MedicalCaseModule.MedicalCaseEntryView
        }
    }
}
