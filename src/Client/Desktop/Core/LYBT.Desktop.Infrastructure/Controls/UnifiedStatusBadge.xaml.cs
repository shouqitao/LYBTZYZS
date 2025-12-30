using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>统一状态标签组件 - 提供统一的状态显示样式</summary>
    public partial class UnifiedStatusBadge : UserControl
    {
        public UnifiedStatusBadge() => InitializeComponent();

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(UnifiedStatusBadge), new PropertyMetadata(string.Empty));

        public BadgeType Type { get => (BadgeType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }
        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(BadgeType), typeof(UnifiedStatusBadge), new PropertyMetadata(BadgeType.Neutral, OnTypeChanged));

        public Brush BadgeBackground { get => (Brush)GetValue(BadgeBackgroundProperty); private set => SetValue(BadgeBackgroundProperty, value); }
        public static readonly DependencyProperty BadgeBackgroundProperty = DependencyProperty.Register(nameof(BadgeBackground), typeof(Brush), typeof(UnifiedStatusBadge), new PropertyMetadata(Brushes.LightGray));

        public Brush BadgeForeground { get => (Brush)GetValue(BadgeForegroundProperty); private set => SetValue(BadgeForegroundProperty, value); }
        public static readonly DependencyProperty BadgeForegroundProperty = DependencyProperty.Register(nameof(BadgeForeground), typeof(Brush), typeof(UnifiedStatusBadge), new PropertyMetadata(Brushes.White));

        private static void OnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { if (d is UnifiedStatusBadge badge) badge.UpdateBadgeColor(); }

        private void UpdateBadgeColor()
        {
            (BadgeBackground, BadgeForeground) = Type switch
            {
                BadgeType.Success => ((Brush)TryFindResource("SuccessBrush") ?? new SolidColorBrush(Color.FromRgb(52, 168, 83)), Brushes.White),
                BadgeType.Warning => ((Brush)TryFindResource("WarningBrush") ?? new SolidColorBrush(Color.FromRgb(251, 188, 4)), Brushes.White),
                BadgeType.Danger => ((Brush)TryFindResource("DangerBrush") ?? new SolidColorBrush(Color.FromRgb(234, 67, 53)), Brushes.White),
                BadgeType.Info => ((Brush)TryFindResource("InfoBrush") ?? new SolidColorBrush(Color.FromRgb(66, 133, 244)), Brushes.White),
                _ => ((Brush)TryFindResource("NeutralBrush") ?? new SolidColorBrush(Color.FromRgb(158, 158, 158)), Brushes.White)
            };
        }
    }
}
