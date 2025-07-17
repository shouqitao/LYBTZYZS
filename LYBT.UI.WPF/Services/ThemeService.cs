using MaterialDesignThemes.Wpf;
using LYBT.UI.WPF.Interfaces;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// Default implementation for <see cref="IThemeService"/> using MaterialDesignThemes.
    /// </summary>
    public class ThemeService : IThemeService {
        private readonly PaletteHelper _paletteHelper = new();

        /// <inheritdoc />
        public bool IsDarkTheme {
            get {
                var theme = _paletteHelper.GetTheme();
                return theme.GetBaseTheme() == BaseTheme.Dark;
            }
        }

        /// <inheritdoc />
        public void ToggleTheme() {
            var theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(IsDarkTheme ? BaseTheme.Light : BaseTheme.Dark);
            _paletteHelper.SetTheme(theme);
        }
    }
}
