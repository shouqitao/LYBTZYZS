using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Consultation
{
    /// <summary>
    /// 诊断管理模块 - 组件模式
    /// Issue #1606 Phase 3: 移除ConsultationRepository/ApiClient（已迁移至MedicalCaseRepository聚合根）
    /// OpenSpec: optimize-desktop-code-reuse Phase 3 - 模块边界评估
    ///
    /// 模块定位: MedicalCase流程的Step 2组件
    /// - 提供ConsultationFormView作为诊断填写界面
    /// - 数据通过MedicalCaseDataManager聚合根管理
    /// - 独立模块便于诊断UI的独立维护和测试
    ///
    /// 提供组件:
    /// - ConsultationFormView/ViewModel: 诊断填写界面（Step 2）
    /// - ConsultationService: 诊断服务 (OpenSpec: standardize-service-layer)
    /// - ConsultationValidator: 诊断数据验证
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

            // OpenSpec: standardize-service-layer - 统一使用Service命名
            containerRegistry.Register<Services.ConsultationService>();
            containerRegistry.Register<Services.ConsultationValidator>();

            // OpenSpec: migrate-views-to-role-modules - ConsultationFormView/ConsultationFormViewModel已删除（无调用）
            //  Issue #1463: ConsultationEntryViewModel已迁移到MedicalCaseModule.MedicalCaseEntryViewModel
            //  Issue #1463: ConsultationEntryView已迁移到MedicalCaseModule.MedicalCaseEntryView
        }
    }
}
