using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.WPF.Client.Workbenches.ConsultationWorkbench.ViewModels;
using LYBT.WPF.Client.Workbenches.ConsultationWorkbench.Views;
using LYBT.WPF.Client.Workbenches.ConsultationWorkbench.Services;
using LYBT.WPF.Client.Workbenches.ConsultationWorkbench.Navigation;
using LYBT.WPF.Client.Workbenches.Core;

namespace LYBT.WPF.Client.Workbenches.ConsultationWorkbench
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
            
            // 注册子视图（这些视图将由业务模块提供）
            // 患者管理、看诊管理、医疗案例管理等视图由各自的BusinessModules提供
            
            // TODO: 可以考虑注册一些工作台特定的视图，如今日预约视图等
        }
    }
}