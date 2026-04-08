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

            // OpenSpec: rename-reference-to-management - 管理视图（薄包装，复用业务模块Control）
            // View在角色台，Control在业务模块
            // 权限设计：诊所共享数据-只读参考，医生自创数据-可完整管理
            containerRegistry.RegisterForNavigation<Views.HerbManagementView>();
            containerRegistry.RegisterForNavigation<Views.FormulaManagementView>();
            containerRegistry.RegisterForNavigation<Views.PatientManagementView>();
            containerRegistry.RegisterForNavigation<Views.MedicalCaseManagementView>();
            
            // 注册PendingQueueView
            containerRegistry.RegisterForNavigation<Views.PendingQueueView>();
        }
    }
}
