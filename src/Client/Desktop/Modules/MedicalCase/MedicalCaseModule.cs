using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.MedicalCase.Views;
using LYBT.Shared.Interfaces.Services;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{

    /// <summary>
    /// 医疗案例模块
    /// </summary>
    public class MedicalCaseModule : IModule
    {

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // UltraThink模块自治：注册业务服务接口实现
            containerRegistry.RegisterSingleton<Services.MedicalCaseService>();
            containerRegistry.RegisterSingleton<IMedicalCaseService>(container => container.Resolve<Services.MedicalCaseService>());

            // UltraThink四层架构：注册标准ViewModel
            containerRegistry.RegisterForNavigation<MedicalCaseListView, MedicalCaseListViewModel>();
            containerRegistry.RegisterForNavigation<MedicalCaseManagementView, MedicalCaseManagementViewModel>();
            containerRegistry.RegisterForNavigation<MedicalCaseDetailView, MedicalCaseDetailViewModel>();

            // 注册对话框
            RegisterDialogs(containerRegistry);
        }

        private void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 医疗案例创建对话框
            // containerRegistry.RegisterDialog<CreateMedicalCaseDialog, CreateMedicalCaseViewModel>(); // Temporarily disabled - IDialogAware not implemented due to Prism 9 compatibility issues
        }
    }
}
