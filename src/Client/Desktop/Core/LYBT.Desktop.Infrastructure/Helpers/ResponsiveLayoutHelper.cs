using System.Windows;

namespace LYBT.Desktop.Infrastructure.Helpers;

/// <summary>
/// 响应式布局辅助类
/// 提供屏幕尺寸检测和断点管理
/// OpenSpec: responsive-layout-optimization
/// </summary>
public static class ResponsiveLayoutHelper
{
    /// <summary>
    /// 小屏幕断点 (平板/小笔记本)
    /// </summary>
    public const double SmallScreenWidth = 1024;

    /// <summary>
    /// 中等屏幕断点 (标准笔记本)
    /// </summary>
    public const double MediumScreenWidth = 1366;

    /// <summary>
    /// 大屏幕断点 (桌面显示器)
    /// </summary>
    public const double LargeScreenWidth = 1920;

    /// <summary>
    /// 获取当前屏幕尺寸类别
    /// </summary>
    public static ScreenSizeCategory GetScreenCategory(double width)
    {
        return width switch
        {
            <= SmallScreenWidth => ScreenSizeCategory.Small,
            <= MediumScreenWidth => ScreenSizeCategory.Medium,
            <= LargeScreenWidth => ScreenSizeCategory.Large,
            _ => ScreenSizeCategory.ExtraLarge
        };
    }

    /// <summary>
    /// 根据屏幕宽度计算最佳列数
    /// </summary>
    public static int GetOptimalColumnCount(double width)
    {
        return width switch
        {
            <= SmallScreenWidth => 1,
            <= MediumScreenWidth => 2,
            <= LargeScreenWidth => 3,
            _ => 4
        };
    }

    /// <summary>
    /// 获取推荐的主区域最小宽度
    /// </summary>
    public static double GetRecommendedMasterWidth(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => 240,
            ScreenSizeCategory.Medium => 280,
            ScreenSizeCategory.Large => 320,
            _ => 360
        };
    }

    /// <summary>
    /// 获取推荐的详情区域最小宽度
    /// </summary>
    public static double GetRecommendedDetailWidth(ScreenSizeCategory category)
    {
        return category switch
        {
            ScreenSizeCategory.Small => 400,
            ScreenSizeCategory.Medium => 480,
            ScreenSizeCategory.Large => 560,
            _ => 640
        };
    }
}

/// <summary>
/// 屏幕尺寸类别
/// </summary>
public enum ScreenSizeCategory
{
    /// <summary>
    /// 小屏幕 (平板/小笔记本)
    /// </summary>
    Small,

    /// <summary>
    /// 中等屏幕 (标准笔记本)
    /// </summary>
    Medium,

    /// <summary>
    /// 大屏幕 (桌面显示器)
    /// </summary>
    Large,

    /// <summary>
    /// 超大屏幕 (大屏显示器)
    /// </summary>
    ExtraLarge
}
