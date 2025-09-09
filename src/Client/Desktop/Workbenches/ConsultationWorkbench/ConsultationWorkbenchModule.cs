// UltraThink Phase 3.4: 集成Formula模块功能
using LYBT.Desktop.Formula.Views;
using LYBT.Desktop.Workbench.Consultation.Navigation;
using LYBT.Desktop.Workbench.Consultation.Services;
using LYBT.Desktop.Workbench.Consultation.ViewModels;
using LYBT.Desktop.Workbench.Consultation.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Workbench.Consultation
{

    /// <summary>
    /// 看诊工作台模块
    /// 为医生提供专业的看诊管理界面
    /// </summary>
    public class ConsultationWorkbenchModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 注册ViewModel映射
            ViewModelLocationProvider.Register<ConsultationWorkbenchMainView, ConsultationWorkbenchMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册工作台导航器
            containerRegistry.RegisterSingleton<IConsultationWorkbenchNavigator, ConsultationWorkbenchNavigator>();

            // 注册主视图
            containerRegistry.RegisterForNavigation<ConsultationWorkbenchMainView>();

            // UltraThink Phase 3.4: 注册集成的验方管理功能
            containerRegistry.RegisterForNavigation<FormulaManagementView>();

            // 注册子视图（这些视图将由业务模块提供）
            // 患者管理、看诊管理、医疗案例管理等视图由各自的BusinessModules提供

            // 预留：未来可注册工作台特定视图，如今日预约视图等
        }
    }
}
