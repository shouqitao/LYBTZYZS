namespace LYBT.Desktop.Core.Constants
{

    /// <summary>
    /// 资源文件路径常量
    /// Provides centralized resource path constants for the application
    /// </summary>
    public static class ResourcePaths
    {

        // Base paths
        private const string AssetsBase = "pack://application:,,,/LYBT.Desktop.Shell;component/Assets/";

        private const string ThemesBase = "pack://application:,,,/LYBT.Desktop.Shell;component/Themes/";

        /// <summary>
        /// Icon paths
        /// </summary>
        public static class Icons
        {

            // Application icons
            public const string AppIcon = AssetsBase + "Icons/App/app.ico";

            public const string AppIconSmall = AssetsBase + "Icons/App/app-16.png";
            public const string AppIconMedium = AssetsBase + "Icons/App/app-32.png";
            public const string AppIconLarge = AssetsBase + "Icons/App/app-48.png";

            // Action icons
            public const string Save = AssetsBase + "Icons/Actions/icon-save-24.png";

            public const string Delete = AssetsBase + "Icons/Actions/icon-delete-24.png";
            public const string Edit = AssetsBase + "Icons/Actions/icon-edit-24.png";
            public const string Add = AssetsBase + "Icons/Actions/icon-add-24.png";
            public const string Search = AssetsBase + "Icons/Actions/icon-search-24.png";
            public const string Print = AssetsBase + "Icons/Actions/icon-print-24.png";
            public const string Refresh = AssetsBase + "Icons/Actions/icon-refresh-24.png";

            // Status icons
            public const string Success = AssetsBase + "Icons/Status/icon-success-16.png";

            public const string Warning = AssetsBase + "Icons/Status/icon-warning-16.png";
            public const string Error = AssetsBase + "Icons/Status/icon-error-16.png";
            public const string Info = AssetsBase + "Icons/Status/icon-info-16.png";
        }

        /// <summary>
        /// Image paths
        /// </summary>
        public static class Images
        {

            // Logos
            public const string LogoMain = AssetsBase + "Images/Logos/logo-main.png";

            public const string LogoSmall = AssetsBase + "Images/Logos/logo-small.png";
            public const string LogoText = AssetsBase + "Images/Logos/logo-text.png";

            // Backgrounds
            public const string LoginBackground = AssetsBase + "Images/Backgrounds/login-bg.jpg";

            public const string MainBackground = AssetsBase + "Images/Backgrounds/main-bg.jpg";

            // Illustrations
            public const string EmptyState = AssetsBase + "Images/Illustrations/empty-state.png";

            public const string NoData = AssetsBase + "Images/Illustrations/no-data.png";
            public const string Welcome = AssetsBase + "Images/Illustrations/welcome.png";
        }

        /// <summary>
        /// Theme resource dictionaries
        /// </summary>
        public static class Themes
        {

            // Design system
            public const string Colors = ThemesBase + "Design/Colors.xaml";

            public const string Typography = ThemesBase + "Design/Typography.xaml";
            public const string Spacing = ThemesBase + "Design/Spacing.xaml";
            // 注释：复杂动画系统已移除，小诊所系统无需过度的动画效果

            // Control templates
            public const string ModernButton = ThemesBase + "Controls/ModernButton.xaml";

            public const string ModernTextBox = ThemesBase + "Controls/ModernTextBox.xaml";
        }

        /// <summary>
        /// Get resource URI
        /// </summary>
        /// <param name="relativePath">Relative path from Assets folder</param>
        /// <returns>Pack URI for the resource</returns>
        public static string GetResourceUri(string relativePath)
        {
            return $"{AssetsBase}{relativePath}";
        }

        /// <summary>
        /// Get theme URI
        /// </summary>
        /// <param name="relativePath">Relative path from Themes folder</param>
        /// <returns>Pack URI for the theme resource</returns>
        public static string GetThemeUri(string relativePath)
        {
            return $"{ThemesBase}{relativePath}";
        }
    }
}
