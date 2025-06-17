using LYBT.UI.WPF.Views;
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
            _regionManager.RequestNavigate("MainRegion", nameof(LoginView));
        }

        private void Navigate(string? view) {
            if (!string.IsNullOrWhiteSpace(view)) {
                _regionManager.RequestNavigate("MainRegion", view);
            }
        }
    }
}
