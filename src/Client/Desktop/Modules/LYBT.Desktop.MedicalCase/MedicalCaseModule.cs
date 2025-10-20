using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")] // 病历依赖患者
    [ModuleDependency("PrescriptionsModule")] // Task #1499: 处方编辑器依赖处方模块
    // ✅ 移除ConsultationModule依赖 - MedicalCase是聚合根，不应依赖子实体模块 (Issue #1463)
    public class MedicalCaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ADR-002 架构标准：
            // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
            // - Repository (数据访问层) 由各业务模块自行注册
            containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

            // Phase 3.4: 启用 Prism Dialog 注册
            containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();

            // 注册视图模型 - MVP核心功能
            containerRegistry.Register<ViewModels.MedicalCaseEntryViewModel>();  // Issue #1463: 病案录入
            containerRegistry.Register<ViewModels.PatientSelectionViewModel>();  // Task #1497: 患者选择
            containerRegistry.Register<ViewModels.PrescriptionEditorViewModel>();  // Task #1499: 处方编辑器
            // TODO: 修复编译错误后再启用
            // containerRegistry.Register<MedicalCaseManagementViewModel>();
            // containerRegistry.Register<MedicalCaseListViewModel>();

            // 注册视图用于导航 - 需要对应视图文件存在
            containerRegistry.RegisterForNavigation<Views.MedicalCaseEntryView>();  // Issue #1463: 病案录入视图
            containerRegistry.RegisterForNavigation<Views.MedicalCaseFlowView>();   // Epic #1494 - Task #1496: 医案流程主视图
            containerRegistry.RegisterForNavigation<Views.PatientSelectionView>();  // Task #1497: 患者选择视图
            containerRegistry.RegisterForNavigation<Views.PrescriptionEditorView>();  // Task #1499: 处方编辑器视图
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
            // containerRegistry.RegisterForNavigation<Views.MedicalCaseListView>();
        }
    }
}
