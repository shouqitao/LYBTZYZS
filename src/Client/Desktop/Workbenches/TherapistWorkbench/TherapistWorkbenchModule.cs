using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Workbench.Therapist.Views;
using LYBT.Desktop.Workbench.Therapist.ViewModels;

namespace LYBT.Desktop.Workbench.Therapist
{
    /// <summary>
    /// 理疗师工作台模块
    /// </summary>
    public class TherapistWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            
            // 注册自定义的ViewModel映射
            ViewModelLocationProvider.Register<TherapistMainView, TherapistMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册理疗师工作台主视图
            containerRegistry.RegisterForNavigation<TherapistMainView>();
            
            // 注册占位视图 (暂时注释，待实现)
            // containerRegistry.RegisterForNavigation<TherapyPlanningView>();
            // containerRegistry.RegisterForNavigation<TreatmentRecordView>();
            // containerRegistry.RegisterForNavigation<RehabilitationManagementView>();
            
            // TODO: 注册其他视图和服务
        }
    }
}