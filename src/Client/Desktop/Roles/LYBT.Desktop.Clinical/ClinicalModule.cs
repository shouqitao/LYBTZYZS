using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Clinical
{
    /// <summary>
    /// 医生角色模块
    /// 功能：医生工作台主页，患者选择，诊疗功能导航入口
    /// OpenSpec: refactor-clinical-workflow
    /// </summary>
    [Module(ModuleName = nameof(ClinicalModule))]
    [ModuleDependency("PatientsModule")]
    [ModuleDependency("MedicalCaseModule")]
    public class ClinicalModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图模型
            containerRegistry.Register<ViewModels.ClinicalHomeViewModel>();
            containerRegistry.Register<ViewModels.PatientSelectionViewModel>();
            containerRegistry.Register<ViewModels.MedicalCaseWorkspaceViewModel>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.ClinicalHomeView>();
            containerRegistry.RegisterForNavigation<Views.PatientSelectionView>();
            containerRegistry.RegisterForNavigation<Views.MedicalCaseWorkspaceView>();

            // OpenSpec: refactor-admin-workspace - 参考视图（薄包装，复用业务模块Control）
            // View在角色台，Control在业务模块
            containerRegistry.RegisterForNavigation<Views.HerbReferenceView>();
            containerRegistry.RegisterForNavigation<Views.FormulaReferenceView>();
            containerRegistry.RegisterForNavigation<Views.PatientHistoryView>();
            containerRegistry.RegisterForNavigation<Views.MedicalCaseArchiveView>();
        }
    }
}
