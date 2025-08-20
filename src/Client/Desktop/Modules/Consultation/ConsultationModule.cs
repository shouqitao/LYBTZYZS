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
            // UltraThink模块化架构：注册模块业务服务
            containerRegistry.RegisterSingleton<ConsultationModuleService>();
            
            // UltraThink简化：只注册核心服务，移除凗余的管理器
            containerRegistry.Register<ConsultationDataService>();
            // 移除了以下服务（功能可以整合到ConsultationModuleService中）：
            // - PrescriptionManager (已有全局PrescriptionService)
            // - FormulaManager (已有全局FormulaService)
            // - ConsultationValidator (可以在Service层做验证)
            containerRegistry.Register<ConsultationEventHandler>();

            // 注册视图模型
            containerRegistry.Register<ConsultationMainViewModel>();

            // 注册视图导航
            containerRegistry.RegisterForNavigation<ConsultationMainView>();
        }
    }
}