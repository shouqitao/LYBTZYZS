using Xunit;

namespace LYBT.Tests.Configuration.Wpf;

/// <summary>
/// WPF测试Collection定义
/// 所有WPF相关测试应使用此Collection以确保测试隔离
/// </summary>
/// <remarks>
/// WPF的Application.Current是静态单例，多个测试并行访问会导致冲突。
/// 使用Collection可以确保同一Collection内的测试顺序执行。
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public class WpfTestCollection : ICollectionFixture<WpfTestFixture>
{
    public const string Name = "WPF Tests";
}

/// <summary>
/// WPF测试Fixture - 管理共享的WPF Application实例
/// </summary>
public class WpfTestFixture : IDisposable
{
    private static readonly object _initLock = new();
    private static bool _isInitialized;

    public WpfTestFixture()
    {
        InitializeWpfApplication();
    }

    /// <summary>
    /// 初始化WPF Application（线程安全）
    /// </summary>
    private static void InitializeWpfApplication()
    {
        lock (_initLock)
        {
            if (_isInitialized)
                return;

#if NET8_0_WINDOWS
            // 仅在Windows目标框架下初始化WPF
            if (System.Windows.Application.Current == null)
            {
                // 创建最小化的Application实例
                var app = new System.Windows.Application
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
                };

                // 初始化基础资源
                InitializeMinimalResources(app);
            }
#endif
            _isInitialized = true;
        }
    }

#if NET8_0_WINDOWS
    /// <summary>
    /// 初始化最小化的WPF资源字典
    /// </summary>
    private static void InitializeMinimalResources(System.Windows.Application app)
    {
        var whiteBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        var grayBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
        var blueBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

        app.Resources = new System.Windows.ResourceDictionary
        {
            // 样式资源
            ["BaseDataGridStyle"] = new System.Windows.Style(typeof(System.Windows.Controls.DataGrid)),
            ["ToolBarContainer"] = new System.Windows.Style(typeof(System.Windows.Controls.Border)),
            ["SearchTextBox"] = new System.Windows.Style(typeof(System.Windows.Controls.TextBox)),
            ["SecondaryButton"] = new System.Windows.Style(typeof(System.Windows.Controls.Button)),
            ["FilterComboBox"] = new System.Windows.Style(typeof(System.Windows.Controls.ComboBox)),
            ["PaginationControlButton"] = new System.Windows.Style(typeof(System.Windows.Controls.Button)),
            ["PaginationCurrentPage"] = new System.Windows.Style(typeof(System.Windows.Controls.Border)),
            ["PaginationPageNumber"] = new System.Windows.Style(typeof(System.Windows.Controls.TextBlock)),

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
            ["StandardMargin"] = new System.Windows.Thickness(4)
        };
    }
#endif

    public void Dispose()
    {
        // Application.Current不需要显式清理
        // 它会在进程结束时自动清理
    }
}
