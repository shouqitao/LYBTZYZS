using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Reports.Repositories;
using LYBT.Desktop.MedicalCase.Reports.Services;
using LYBT.Desktop.MedicalCase.Reports.ViewModels;
using LYBT.Desktop.MedicalCase.Reports.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Reports;

// STUB: 报表模块 - 当前为存根实现，仅提供基础导航
// TODO 2026-08-21 xiao: 后续迭代完善报表功能（当前仅基础导航，趋势/绩效待迭代）
[Module(ModuleName = nameof(ReportsModule))]
public class ReportsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // STUB: 模块初始化 - 暂无操作
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // P0-2: 报表 Repository Singleton（无状态只读聚合），Service 经 Repository 访问 IApiClient.Reports
        containerRegistry.RegisterSingleton<IReportRepository, ReportRepository>();
        containerRegistry.Register<IReportService, ReportService>();
        // STUB: 注册基础视图和视图模型
        ViewModelLocationProvider.Register(typeof(ReportsHomeView).ToString(), typeof(ReportsHomeViewModel));
        containerRegistry.RegisterForNavigation<ReportsHomeView>();
    }
}
