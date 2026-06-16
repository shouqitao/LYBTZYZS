using LYBT.Desktop.Reports.ViewModels;
using LYBT.Desktop.Reports.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Reports;

[Module(ModuleName = nameof(ReportsModule))]
public class ReportsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        ViewModelLocationProvider.Register(typeof(ReportsHomeView).ToString(), typeof(ReportsHomeViewModel));
        containerRegistry.RegisterForNavigation<ReportsHomeView>();
    }
}
