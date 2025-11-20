using System.Windows;
using LYBT.Desktop.Infrastructure.Converters;

namespace LYBT.Desktop.Infrastructure.Tests;

/// <summary>
/// WPF测试初始化器 - 负责初始化Application资源字典
/// Issue #2153 - Task 1.3: 控件层单元测试
/// </summary>
public static class WpfTestInitializer
{
    private static bool _isInitialized;
    private static readonly object _lock = new();

    /// <summary>
    /// 初始化WPF Application和资源字典
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;

            // 创建Application实例（如果不存在）
            if (Application.Current == null)
            {
                _ = new Application();
            }

            // 加载基础资源字典
            var app = Application.Current;
            if (app != null)
            {
                // 创建最小化的资源字典用于测试
                // 避免加载完整的App.xaml资源
                var whiteBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var grayBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blueBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                var baseTextBoxStyle = new Style(typeof(System.Windows.Controls.TextBox));
                var baseButtonStyle = new Style(typeof(System.Windows.Controls.Button));
                var baseComboBoxStyle = new Style(typeof(System.Windows.Controls.ComboBox));

                app.Resources = new ResourceDictionary
                {
                    // 样式资源
                    ["BaseDataGridStyle"] = CreateBaseDataGridStyle(),
                    ["ToolBarContainer"] = CreateToolBarContainerStyle(),
                    ["SearchTextBox"] = baseTextBoxStyle,
                    ["SecondaryButton"] = baseButtonStyle,
                    ["FilterComboBox"] = baseComboBoxStyle,
                    ["PaginationControlButton"] = baseButtonStyle,

                    // 颜色画刷资源
                    ["BackgroundBrush"] = whiteBrush,
                    ["PrimaryBrush"] = blueBrush,
                    ["BorderBrush"] = grayBrush,
                    ["NeutralBrush"] = grayBrush,
                    ["NeutralLightBrush"] = grayBrush,
                    ["EmptyStateBackground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray),
                    ["EmptyStateForeground"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkGray),

                    // 字体大小资源
                    ["FontSizeDisplay"] = 14.0,
                    ["FontSizeBody"] = 13.0,
                    ["FontSizeLabel"] = 12.0,
                    ["FontSizeTitle"] = 16.0,
                    ["FontSizeSmall"] = 11.0,

                    // 间距资源
                    ["SpacingSmall"] = new System.Windows.Thickness(4),
                    ["SpacingMedium"] = new System.Windows.Thickness(8),
                    ["SpacingLarge"] = new System.Windows.Thickness(16),

                    // 其他常用资源
                    ["CornerRadius"] = new System.Windows.CornerRadius(4),
                    ["StandardPadding"] = new System.Windows.Thickness(8),
                    ["StandardMargin"] = new System.Windows.Thickness(4),

                    // 转换器
                    ["InverseNullToVisibilityConverter"] = new InverseNullToVisibilityConverter(),
                    ["NullToVisibilityConverter"] = new Converters.NullToVisibilityConverter(),

                    // 分页控件样式
                    ["PaginationCurrentPage"] = CreatePaginationCurrentPageStyle(),
                    ["PaginationPageNumber"] = CreatePaginationPageNumberStyle()
                };
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// 创建用于测试的基础DataGrid样式
    /// </summary>
    private static Style CreateBaseDataGridStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.DataGrid));
        style.Setters.Add(new Setter(System.Windows.Controls.DataGrid.AutoGenerateColumnsProperty, false));
        style.Setters.Add(new Setter(System.Windows.Controls.DataGrid.CanUserAddRowsProperty, false));
        style.Setters.Add(new Setter(System.Windows.Controls.DataGrid.CanUserDeleteRowsProperty, false));
        return style;
    }

    /// <summary>
    /// 创建用于测试的ToolBarContainer样式
    /// </summary>
    private static Style CreateToolBarContainerStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.Border));
        return style;
    }

    /// <summary>
    /// 创建用于测试的PaginationCurrentPage样式
    /// </summary>
    private static Style CreatePaginationCurrentPageStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.Border));
        style.Setters.Add(new Setter(System.Windows.Controls.Border.PaddingProperty, new System.Windows.Thickness(8, 4, 8, 4)));
        return style;
    }

    /// <summary>
    /// 创建用于测试的PaginationPageNumber样式
    /// </summary>
    private static Style CreatePaginationPageNumberStyle()
    {
        var style = new Style(typeof(System.Windows.Controls.TextBlock));
        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, 12.0));
        return style;
    }
}
