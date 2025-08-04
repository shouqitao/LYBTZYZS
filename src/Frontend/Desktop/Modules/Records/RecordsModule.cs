using LYBT.WPF.Client.Modules.Records.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Records
{
    /// <summary>
    /// 病历管理模块
    /// </summary>
    public class RecordsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册病历管理视图
            containerRegistry.RegisterForNavigation<RecordManagementView>();
        }
    }
}