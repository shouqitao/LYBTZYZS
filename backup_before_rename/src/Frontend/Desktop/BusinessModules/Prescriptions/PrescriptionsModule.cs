using LYBT.Desktop.Prescriptions.Shared.Services;
using LYBT.Desktop.Prescriptions.Shared.ViewModels;
using LYBT.Desktop.Prescriptions.Shared.Views;
using LYBT.Desktop.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Prescriptions.Shared
{
    /// <summary>
    /// 处方管理业务模块
    /// </summary>
    public class PrescriptionsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<PrescriptionManagementView, PrescriptionManagementViewModel>();

            // 注册服务
            containerRegistry.RegisterSingleton<ISharedPrescriptionService, SharedPrescriptionService>();

            // 注册对话框
            containerRegistry.RegisterDialog<PrescriptionAddEditDialog, PrescriptionAddEditDialogViewModel>();
            containerRegistry.RegisterDialog<PrescriptionDetailDialog, PrescriptionDetailDialogViewModel>();
        }
    }
}