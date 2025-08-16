using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Consultation.Views;
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Consultation.Services.Interfaces;
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
            // 注册看诊模块内部服务
            containerRegistry.Register<IConsultationDataService, ConsultationDataService>();
            containerRegistry.Register<IPrescriptionManager, PrescriptionManager>();
            containerRegistry.Register<IFormulaManager, FormulaManager>();
            containerRegistry.Register<IConsultationValidator, ConsultationValidator>();
            containerRegistry.Register<IConsultationEventHandler, ConsultationEventHandler>();

            // 注册视图模型
            containerRegistry.Register<ConsultationMainViewModel>();

            // 注册视图导航
            containerRegistry.RegisterForNavigation<ConsultationMainView>();
        }
    }
}