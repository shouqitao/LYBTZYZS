using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Prescriptions.Services;
using LYBT.Desktop.Services.Print;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方管理模块 - 精简版
    /// OpenSpec: refactor-prescription-module-consolidation - 删除所有未使用的对话框
    /// 模块职责: 只提供处方相关服务，UI功能已迁移到MedicalCase模块
    /// </summary>
    [Module(ModuleName = nameof(PrescriptionsModule))]
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
            // === 核心服务注册 ===

            // 注册打印服务（Issue #1381: PRINT-4）
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();

            // Epic #1540: 注册处方编辑器服务（方案B - 包装模式）
            // 实现依赖倒置：MedicalCase模块依赖IPrescriptionEditorService接口
            containerRegistry.RegisterSingleton<IPrescriptionEditorService, PrescriptionEditorService>();

            // === 已删除的死代码 ===
            // OpenSpec: refactor-prescription-module-consolidation
            // [已删除] FormulaTemplateDialog - 无调用入口
            // [已删除] SelectFormulaDialog - 无调用入口
            // [已删除] PrescriptionEditorDialog - 无调用入口
            // [已删除] PrescriptionManagementView - 功能已迁移到MedicalCase
            // [已删除] PrescriptionViewModel - 功能已迁移到MedicalCase
        }
    }
}
