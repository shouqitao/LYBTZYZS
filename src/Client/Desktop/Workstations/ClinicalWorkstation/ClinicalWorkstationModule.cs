using LYBT.Desktop.ClinicalWorkstation.ViewModels;
using LYBT.Desktop.ClinicalWorkstation.Views;
using LYBT.Desktop.Services.ErrorHandling;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.ClinicalWorkstation
{
    /// <summary>
    /// 诊疗工作台模块
    /// 提供医生的诊疗工作台界面和相关功能
    /// </summary>
    [Module(ModuleName = nameof(ClinicalWorkstationModule))]
    [ModuleDependency("AuthenticationModule")] // 依赖认证模块
    public class ClinicalWorkstationModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            var logger = containerProvider.Resolve<ILogger<ClinicalWorkstationModule>>();
            logger?.LogInformation("诊疗工作台模块初始化完成");
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图模型
            containerRegistry.Register<ClinicalWorkstationViewModel>();

            // 注册主视图用于导航
            containerRegistry.RegisterForNavigation<ClinicalWorkstationView>();

            // 注册诊疗功能子视图（后续根据需要添加）
            // containerRegistry.RegisterForNavigation<DiagnosisView>();
            // containerRegistry.RegisterForNavigation<PrescriptionView>();
            // containerRegistry.RegisterForNavigation<PatientHistoryView>();
            // containerRegistry.RegisterForNavigation<HerbSearchView>();
        }
    }
}