using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 状态标签类型枚举
    /// </summary>
    public enum BadgeType
    {
        /// <summary>成功状态</summary>
        Success,
        /// <summary>警告状态</summary>
        Warning,
        /// <summary>危险状态</summary>
        Danger,
        /// <summary>信息状态</summary>
        Info,
        /// <summary>中性状态</summary>
        Neutral
    }

    /// <summary>
    /// 统一状态标签组件
    /// 提供统一的状态显示样式
    /// Issue #1840 - Desktop端管理界面UI统一化
    /// </summary>
    public partial class UnifiedStatusBadge : UserControl
    {
        public UnifiedStatusBadge()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 状态文本
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(UnifiedStatusBadge),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// 标签类型
        /// </summary>
        public BadgeType Type
        {
            get => (BadgeType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(BadgeType),
                typeof(UnifiedStatusBadge),
                new PropertyMetadata(BadgeType.Neutral, OnTypeChanged));

        private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UnifiedStatusBadge badge)
            {
                badge.UpdateBadgeColor();
            }
        }

        /// <summary>
        /// 标签背景色
        /// </summary>
        public Brush BadgeBackground
        {
            get => (Brush)GetValue(BadgeBackgroundProperty);
            private set => SetValue(BadgeBackgroundProperty, value);
        }

        public static readonly DependencyProperty BadgeBackgroundProperty =
            DependencyProperty.Register(
                nameof(BadgeBackground),
                typeof(Brush),
                typeof(UnifiedStatusBadge),
                new PropertyMetadata(Brushes.LightGray));

        /// <summary>
        /// 标签前景色
        /// </summary>
        public Brush BadgeForeground
        {
            get => (Brush)GetValue(BadgeForegroundProperty);
            private set => SetValue(BadgeForegroundProperty, value);
        }

        public static readonly DependencyProperty BadgeForegroundProperty =
            DependencyProperty.Register(
                nameof(BadgeForeground),
                typeof(Brush),
                typeof(UnifiedStatusBadge),
                new PropertyMetadata(Brushes.White));

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据类型更新标签颜色
        /// </summary>
        private void UpdateBadgeColor()
        {
            switch (Type)
            {
                case BadgeType.Success:
                    BadgeBackground = (Brush)TryFindResource("SuccessBrush") ?? new SolidColorBrush(Color.FromRgb(52, 168, 83));
                    BadgeForeground = Brushes.White;
                    break;
                case BadgeType.Warning:
                    BadgeBackground = (Brush)TryFindResource("WarningBrush") ?? new SolidColorBrush(Color.FromRgb(251, 188, 4));
                    BadgeForeground = Brushes.White;
                    break;
                case BadgeType.Danger:
                    BadgeBackground = (Brush)TryFindResource("DangerBrush") ?? new SolidColorBrush(Color.FromRgb(234, 67, 53));
                    BadgeForeground = Brushes.White;
                    break;
                case BadgeType.Info:
                    BadgeBackground = (Brush)TryFindResource("InfoBrush") ?? new SolidColorBrush(Color.FromRgb(66, 133, 244));
                    BadgeForeground = Brushes.White;
                    break;
                case BadgeType.Neutral:
                default:
                    BadgeBackground = (Brush)TryFindResource("NeutralBrush") ?? new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    BadgeForeground = Brushes.White;
                    break;
            }
        }

        #endregion
    }
}
