using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Controls;

/// <summary>
/// 状态徽章控件
/// OpenSpec: refactor-master-detail-layout - UI优化
/// 以彩色徽章形式显示状态，替代纯文本状态显示
/// </summary>
public partial class StatusBadge : UserControl
{
    #region 依赖属性

    /// <summary>状态值（支持字符串或枚举）</summary>
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(object),
            typeof(StatusBadge),
            new PropertyMetadata(null, OnStatusChanged));

    /// <summary>自定义状态文本（优先于Status自动映射）</summary>
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(
            nameof(StatusText),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(null, OnStatusChanged));

    /// <summary>徽章类型（Success/Danger/Warning/Info/Neutral）</summary>
    public static readonly DependencyProperty BadgeTypeProperty =
        DependencyProperty.Register(
            nameof(BadgeType),
            typeof(BadgeType),
            typeof(StatusBadge),
            new PropertyMetadata(BadgeType.Neutral, OnBadgeTypeChanged));

    /// <summary>显示文本（只读）</summary>
    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(
            nameof(DisplayText),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(string.Empty));

    /// <summary>徽章背景色（只读）</summary>
    public static readonly DependencyProperty BadgeBackgroundProperty =
        DependencyProperty.Register(
            nameof(BadgeBackground),
            typeof(Brush),
            typeof(StatusBadge),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5))));

    /// <summary>徽章前景色（只读）</summary>
    public static readonly DependencyProperty BadgeForegroundProperty =
        DependencyProperty.Register(
            nameof(BadgeForeground),
            typeof(Brush),
            typeof(StatusBadge),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61))));

    #endregion

    #region 属性

    public object? Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string? StatusText
    {
        get => (string?)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public BadgeType BadgeType
    {
        get => (BadgeType)GetValue(BadgeTypeProperty);
        set => SetValue(BadgeTypeProperty, value);
    }

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        private set => SetValue(DisplayTextProperty, value);
    }

    public Brush BadgeBackground
    {
        get => (Brush)GetValue(BadgeBackgroundProperty);
        private set => SetValue(BadgeBackgroundProperty, value);
    }

    public Brush BadgeForeground
    {
        get => (Brush)GetValue(BadgeForegroundProperty);
        private set => SetValue(BadgeForegroundProperty, value);
    }

    #endregion

    public StatusBadge()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    #region 属性变更处理

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
        {
            badge.UpdateDisplay();
        }
    }

    private static void OnBadgeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
        {
            badge.UpdateColors();
        }
    }

    #endregion

    #region 私有方法

    private void UpdateDisplay()
    {
        // 确定显示文本
        if (!string.IsNullOrEmpty(StatusText))
        {
            DisplayText = StatusText;
        }
        else if (Status != null)
        {
            var statusStr = Status.ToString() ?? string.Empty;
            DisplayText = MapStatusToDisplayText(statusStr);

            // 自动确定BadgeType（如果没有显式设置）
            BadgeType = MapStatusToBadgeType(statusStr);
        }
        else
        {
            DisplayText = "-";
            BadgeType = BadgeType.Neutral;
        }

        UpdateColors();
    }

    private void UpdateColors()
    {
        var (background, foreground) = GetBadgeColors(BadgeType);
        BadgeBackground = background;
        BadgeForeground = foreground;
    }

    private static string MapStatusToDisplayText(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "enabled" => "启用",
            "disabled" => "禁用",
            "active" => "活跃",
            "inactive" => "不活跃",
            "pending" => "待处理",
            "completed" => "已完成",
            "cancelled" => "已取消",
            "draft" => "草稿",
            "published" => "已发布",
            "archived" => "已归档",
            "true" => "是",
            "false" => "否",
            _ => status
        };
    }

    private static BadgeType MapStatusToBadgeType(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "enabled" or "active" or "completed" or "published" or "true" => BadgeType.Success,
            "disabled" or "inactive" or "cancelled" or "archived" or "false" => BadgeType.Danger,
            "pending" or "draft" => BadgeType.Warning,
            _ => BadgeType.Neutral
        };
    }

    private static (Brush background, Brush foreground) GetBadgeColors(BadgeType type)
    {
        return type switch
        {
            BadgeType.Success => (
                new SolidColorBrush(Color.FromRgb(0xE6, 0xF4, 0xEA)),
                new SolidColorBrush(Color.FromRgb(0x1E, 0x7D, 0x34))),
            BadgeType.Danger => (
                new SolidColorBrush(Color.FromRgb(0xFC, 0xE8, 0xE8)),
                new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C))),
            BadgeType.Warning => (
                new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xCE)),
                new SolidColorBrush(Color.FromRgb(0x9D, 0x5D, 0x00))),
            BadgeType.Info => (
                new SolidColorBrush(Color.FromRgb(0xE5, 0xF6, 0xFD)),
                new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4))),
            _ => (
                new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)))
        };
    }

    #endregion
}

// BadgeType 枚举定义在 UnifiedStatusBadge.xaml.cs 中，两个控件共享
