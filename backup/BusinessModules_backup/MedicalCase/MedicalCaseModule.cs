using LYBT.Desktop.MedicalCase.Shared.Services;
using LYBT.Desktop.MedicalCase.Shared.ViewModels;
using LYBT.Desktop.MedicalCase.Shared.Views;
using LYBT.Desktop.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.MedicalCase.Shared
{
    /// <summary>
    /// 医疗案例管理业务模块
    /// </summary>
    public class MedicalCaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<MedicalCaseManagementView, MedicalCaseManagementViewModel>();

            // 注册服务
            containerRegistry.RegisterSingleton<ISharedMedicalCaseService, SharedMedicalCaseService>();

            // 注册对话框
            containerRegistry.RegisterDialog<MedicalCaseDialog, MedicalCaseDialogViewModel>();
            containerRegistry.RegisterDialog<MedicalRecordsDialog, MedicalRecordsDialogViewModel>();
        }
    }
}