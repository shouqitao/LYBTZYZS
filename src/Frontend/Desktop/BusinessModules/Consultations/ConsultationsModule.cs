using LYBT.WPF.Client.BusinessModules.Consultations.Services;
using LYBT.WPF.Client.BusinessModules.Consultations.ViewModels;
using LYBT.WPF.Client.BusinessModules.Consultations.Views;
using LYBT.WPF.Client.BusinessModules.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.BusinessModules.Consultations
{
    /// <summary>
    /// 看诊管理业务模块
    /// </summary>
    public class ConsultationsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型
            containerRegistry.RegisterForNavigation<ConsultationManagementView, ConsultationManagementViewModel>();

            // 注册服务
            containerRegistry.RegisterSingleton<ISharedConsultationService, SharedConsultationService>();

            // 注册对话框
            containerRegistry.RegisterDialog<ConsultationDialog, ConsultationDialogViewModel>();
            containerRegistry.RegisterDialog<FourExaminationsDialog, FourExaminationsDialogViewModel>();
        }
    }
}