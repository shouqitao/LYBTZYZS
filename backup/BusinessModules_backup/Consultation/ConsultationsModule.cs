using LYBT.Desktop.Consultation.Shared.Services;
using LYBT.Desktop.Consultation.Shared.ViewModels;
using LYBT.Desktop.Consultation.Shared.Views;
using LYBT.Desktop.Shared;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Consultation.Shared
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