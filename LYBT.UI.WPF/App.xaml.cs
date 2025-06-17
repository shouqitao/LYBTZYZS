using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System.Windows;
using LYBT.UI.WPF.Views;

namespace LYBT.UI.WPF {
    /// <summary>
    /// Prism application bootstrap
    /// </summary>
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            return Container.Resolve<ShellView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterForNavigation<LoginView>(nameof(LoginView));
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) {
            // register modules here
        }
    }
}
