using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows.Input;

namespace LYBT.UI.WPF.ViewModels {
    /// <summary>
    /// View model for the main shell window
    /// </summary>
    public class ShellViewModel : BindableBase {
        private readonly IRegionManager _regionManager;
        public ICommand NavigateCommand { get; }

        public ShellViewModel(IRegionManager regionManager) {
            _regionManager = regionManager;
            NavigateCommand = new DelegateCommand<string?>(Navigate);
        }

        private void Navigate(string? view) {
            if (!string.IsNullOrWhiteSpace(view)) {
                _regionManager.RequestNavigate("MainRegion", view);
            }
        }
    }
}
