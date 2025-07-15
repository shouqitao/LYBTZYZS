using Prism.Commands;
using Prism.Mvvm;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.ViewModels.Base {
    /// <summary>
    /// Provides common properties for profile view models.
    /// </summary>
    public abstract class BaseProfileViewModel : BindableBase {
        private ProfileMode _mode;
        /// <summary>Current profile mode.</summary>
        public ProfileMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        private bool _isEditable;
        /// <summary>Indicates whether the profile can be edited.</summary>
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        /// <summary>Command for persisting changes.</summary>
        public DelegateCommand? SaveCommand { get; protected set; }
        /// <summary>Command for canceling editing.</summary>
        public DelegateCommand? CancelCommand { get; protected set; }
    }
}
