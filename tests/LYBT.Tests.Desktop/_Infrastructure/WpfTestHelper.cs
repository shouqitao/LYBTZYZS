using System.Windows;
using LYBT.Desktop.Infrastructure.Converters;

namespace LYBT.Tests.Desktop.Infrastructure;

/// <summary>
/// Minimal WPF initialization helper for tests that need Application.Current resources.
/// Replaces the heavyweight DesktopFixture.InitializeWpf() with only resource setup.
/// </summary>
public static class WpfTestHelper
{
    private static readonly object WpfLock = new();
    private static bool _initialized;

    public static void InitializeWpf()
    {
        lock (WpfLock)
        {
            if (_initialized) return;

            if (Application.Current == null)
            {
                _ = new Application();
            }

            var app = Application.Current;
            if (app != null)
            {
                var whiteBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var grayBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blueBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                app.Resources = new ResourceDictionary
                {
                    ["BaseDataGridStyle"] = new Style(typeof(System.Windows.Controls.DataGrid)),
                    ["BaseDataGridCell"] = new Style(typeof(System.Windows.Controls.DataGridCell)),
                    ["BaseDataGridRow"] = new Style(typeof(System.Windows.Controls.DataGridRow)),
                    ["BaseDataGridColumnHeader"] = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)),
                    ["ToolBarContainer"] = new Style(typeof(System.Windows.Controls.Border)),
                    ["SearchTextBox"] = new Style(typeof(System.Windows.Controls.TextBox)),
                    ["SecondaryButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["FilterComboBox"] = new Style(typeof(System.Windows.Controls.ComboBox)),
                    ["PaginationControlButton"] = new Style(typeof(System.Windows.Controls.Button)),
                    ["BackgroundBrush"] = whiteBrush,
                    ["PrimaryBrush"] = blueBrush,
                    ["BorderBrush"] = grayBrush,
                    ["NeutralBrush"] = grayBrush,
                    ["NeutralLightBrush"] = grayBrush,
                    ["RegionBrush"] = whiteBrush,
                    ["EmptyStateBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
                    ["EmptyStateForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),
                    ["FontSizeDisplay"] = 14.0,
                    ["FontSizeBody"] = 13.0,
                    ["FontSizeLabel"] = 12.0,
                    ["FontSizeTitle"] = 16.0,
                    ["FontSizeSmall"] = 11.0,
                    ["SpacingSmall"] = new Thickness(4),
                    ["SpacingMedium"] = new Thickness(8),
                    ["SpacingLarge"] = new Thickness(16),
                    ["CornerRadius"] = new CornerRadius(4),
                    ["StandardPadding"] = new Thickness(8),
                    ["StandardMargin"] = new Thickness(4),
                    ["InverseNullToVisibilityConverter"] = new InverseNullToVisibilityConverter(),
                    ["NullToVisibilityConverter"] = new NullToVisibilityConverter(),
                    ["PaginationCurrentPage"] = new Style(typeof(System.Windows.Controls.Border)),
                    ["PaginationPageNumber"] = new Style(typeof(System.Windows.Controls.TextBlock)),
                    ["PageSizeOptions"] = new int[] { 10, 20, 50, 100 },
                };
            }

            _initialized = true;
        }
    }
}
