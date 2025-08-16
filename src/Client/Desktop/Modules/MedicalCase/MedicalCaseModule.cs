using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Desktop.MedicalCase.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例模块
    /// </summary>
    public class MedicalCaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图导航
            containerRegistry.RegisterForNavigation<MedicalCaseListView, MedicalCaseListViewModel>();
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