using LYBT.Desktop.Reports.ViewModels;
using LYBT.Desktop.Reports.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Reports;

// STUB: 报表模块 - 当前为存根实现，仅提供基础导航
// TODO: 后续迭代完善报表功能
[Module(ModuleName = nameof(ReportsModule))]
public class ReportsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // STUB: 模块初始化 - 暂无操作
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // STUB: 注册基础视图和视图模型
        ViewModelLocationProvider.Register(typeof(ReportsHomeView).ToString(), typeof(ReportsHomeViewModel));
        containerRegistry.RegisterForNavigation<ReportsHomeView>();
    }
}
