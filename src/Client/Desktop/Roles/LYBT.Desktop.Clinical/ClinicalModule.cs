using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Clinical
{
    /// <summary>
    /// 医生角色模块
    /// 功能：医生工作台主页，提供诊疗功能导航入口
    /// </summary>
    [Module(ModuleName = nameof(ClinicalModule))]
    public class ClinicalModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图模型
            containerRegistry.Register<ViewModels.ClinicalHomeViewModel>();

            // 注册视图用于导航
            containerRegistry.RegisterForNavigation<Views.ClinicalHomeView>();
        }
    }
}
