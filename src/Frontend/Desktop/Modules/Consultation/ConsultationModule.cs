using Prism.Ioc;
using Prism.Modularity;
using LYBT.WPF.Client.Modules.Consultation.Views;

namespace LYBT.WPF.Client.Modules.Consultation
{
    /// <summary>
    /// 看诊模块 - 简化版（无外部依赖）
    /// </summary>
    public class ConsultationModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图
            containerRegistry.RegisterForNavigation<ConsultationMainView>();
        }
    }
}