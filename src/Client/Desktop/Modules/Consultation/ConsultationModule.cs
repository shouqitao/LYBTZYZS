using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Consultation.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Consultation
{

    /// <summary>
    /// 看诊诊断模块 - UltraThink双层架构Prism模块
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：看诊诊断模块依赖注入、服务注册、视图导航配置
    /// 实现中医四诊（望闻问切）、辨证论治、诊断记录等核心功能
    /// 集成双层架构服务（QueryService + BusinessService + Module委托）
    /// 适配中医诊所诊断流程，确保诊疗数据安全和功能完整性
    /// </summary>
    public class ConsultationModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成后，配置ViewModelLocator
            ViewModelLocationProvider.Register<ConsultationMainView, ConsultationMainViewModel>();
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink双层架构服务注册
            containerRegistry.RegisterSingleton<LYBT.Desktop.Consultation.Interfaces.IConsultationQueryService, ConsultationQueryService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Consultation.Interfaces.IConsultationBusinessService, ConsultationBusinessService>();

            // UltraThink纯委托主服务注册
            containerRegistry.RegisterSingleton<Services.ConsultationService>();
            containerRegistry.RegisterSingleton<IConsultationService>(container => container.Resolve<Services.ConsultationService>());

            // 注册简化后的视图模型
            containerRegistry.Register<ConsultationMainViewModel>();
            containerRegistry.Register<ConsultationManagementViewModel>();

            // 注册视图导航
            containerRegistry.RegisterForNavigation<ConsultationMainView>();
            containerRegistry.RegisterForNavigation<ConsultationManagementView, ConsultationManagementViewModel>();
        }
    }
}
