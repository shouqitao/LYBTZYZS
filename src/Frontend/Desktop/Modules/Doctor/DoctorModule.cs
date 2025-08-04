using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Doctor.Views;

namespace LYBT.WPF.Client.Modules.Doctor
{
    /// <summary>
    /// 医生模块
    /// </summary>
    public class DoctorModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // TODO: 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册医生主界面视图
            containerRegistry.RegisterForNavigation<DoctorMainView>();
            
            // 注册诊疗医生主界面视图
            containerRegistry.RegisterForNavigation<DiagnosingDoctorMainView>();
            
            // 注册看诊管理视图
            containerRegistry.RegisterForNavigation<ConsultationManagementView>();
        }
    }
}