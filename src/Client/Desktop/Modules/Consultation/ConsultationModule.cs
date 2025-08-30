using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Consultation.Views;
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Consultation.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Desktop.Consultation
{
    /// <summary>
    /// 看诊模块
    /// </summary>
    public class ConsultationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后，配置ViewModelLocator
            ViewModelLocationProvider.Register<ConsultationMainView, ConsultationMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 简化后的服务注册 - 只保留纯数据记录功能
            containerRegistry.RegisterSingleton<Services.ConsultationModule>();

            // 注册简化后的视图模型
            containerRegistry.Register<ConsultationMainViewModel>();
            containerRegistry.Register<ConsultationManagementViewModel>();

            // 注册视图导航
            containerRegistry.RegisterForNavigation<ConsultationMainView>();
            containerRegistry.RegisterForNavigation<ConsultationManagementView, ConsultationManagementViewModel>();
        }
    }
}