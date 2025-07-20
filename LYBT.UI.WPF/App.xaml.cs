using LYBT.UI.WPF.Views;
using Prism.Ioc;
using System.Windows;

namespace LYBT.UI.WPF {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {

        }
    }
}
