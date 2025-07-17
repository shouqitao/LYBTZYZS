using System;

namespace LYBT.UI.WPF.Interfaces {
    /// <summary>
    /// Provides theme switching capabilities.
    /// </summary>
    public interface IThemeService {
        /// <summary>
        /// Indicates whether the dark theme is currently active.
        /// </summary>
        bool IsDarkTheme { get; }

        /// <summary>
        /// Toggles between light and dark base themes.
        /// </summary>
        void ToggleTheme();
    }
}
