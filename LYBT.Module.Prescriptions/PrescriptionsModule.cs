using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.ViewModels;
using LYBT.Module.Prescriptions.Views;

namespace LYBT.Module.Prescriptions {

    /// <summary>
    /// 处方模块入口
    /// </summary>
    public class PrescriptionsModule : IModule {

        public void RegisterTypes(IContainerRegistry containerRegistry) {
            // 注册服务和ViewModel
            containerRegistry.RegisterSingleton<IPrescriptionService, PrescriptionService>();
            containerRegistry.Register<PrescriptionListViewModel>();
            containerRegistry.Register<PrescriptionEditViewModel>();
            // 注册视图
            containerRegistry.RegisterForNavigation<PrescriptionListView>();
            containerRegistry.RegisterForNavigation<PrescriptionEditView>();
        }

        public void OnInitialized(IContainerProvider containerProvider) {
            // 可初始化操作
        }
    }
}