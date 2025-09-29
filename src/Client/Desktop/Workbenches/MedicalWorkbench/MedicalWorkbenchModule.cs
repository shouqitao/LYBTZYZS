// UltraThink Phase 3.4: 集成Formula模块功能
using LYBT.Desktop.Formula.Views;
using LYBT.Desktop.Workbench.Medical.Navigation;
using LYBT.Desktop.Workbench.Medical.Services;
using LYBT.Desktop.Workbench.Medical.ViewModels;
using LYBT.Desktop.Workbench.Medical.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Workbench.Medical
{

    /// <summary>
    /// 诊疗工作台模块
    /// 为医生提供专业的诊疗管理界面
    /// </summary>
    [Module(ModuleName = nameof(MedicalWorkbenchModule))]
    [ModuleDependency("PatientsModule")]
    [ModuleDependency("ConsultationModule")]
    [ModuleDependency("MedicalCaseModule")]
    [ModuleDependency("PrescriptionsModule")]
    public class MedicalWorkbenchModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 注册ViewModel映射
            ViewModelLocationProvider.Register<MedicalWorkbenchMainView, MedicalWorkbenchMainViewModel>();
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册工作台导航器
            containerRegistry.RegisterSingleton<IMedicalWorkbenchNavigator, MedicalWorkbenchNavigator>();

            // 注册主视图
            containerRegistry.RegisterForNavigation<MedicalWorkbenchMainView>();

            // UltraThink Phase 3.4: 注册集成的验方管理功能
            containerRegistry.RegisterForNavigation<FormulaManagementView>();

            // 注册子视图（这些视图将由业务模块提供）
            // 患者管理、诊疗管理、医疗案例管理等视图由各自的BusinessModules提供

            // 预留：未来可注册工作台特定视图，如今日预约视图等
        }
    }
}
