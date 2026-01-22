using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 转换器静态实例提供者
    ///
    /// 解决WPF资源架构问题 (OpenSpec: cleanup-control-resource-merging):
    /// - Binding.Converter 不是 DependencyProperty，必须使用 StaticResource
    /// - StaticResource 要求资源在 XAML 解析时已存在
    /// - 当控件被加载到 ContentPresenter（如 MasterDetailLayout）时，资源查找路径可能断裂
    ///
    /// 解决方案：使用 x:Static 引用静态实例，完全绕过资源字典查找机制
    ///
    /// 使用方式:
    /// <code>
    /// xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure"
    ///
    /// Before (问题模式):
    ///   Converter={StaticResource BooleanToVisibilityConverter}
    ///
    /// After (解决方案):
    ///   Converter={x:Static converters:Cvt.BoolToVis}
    /// </code>
    /// </summary>
    public static class Cvt
    {
        // ========== Boolean Converters ==========

        /// <summary>
        /// Bool -> Visibility (true=Visible, false=Collapsed)
        /// </summary>
        public static readonly IValueConverter BoolToVis = new BooleanToVisibilityConverter();

        /// <summary>
        /// Bool -> Visibility (true=Collapsed, false=Visible)
        /// </summary>
        public static readonly IValueConverter InverseBoolToVis = new InverseBooleanToVisibilityConverter();

        /// <summary>
        /// Bool -> !Bool
        /// </summary>
        public static readonly IValueConverter InverseBool = new InverseBooleanConverter();

        /// <summary>
        /// Bool -> Brush (可配置TrueBrush/FalseBrush)
        /// </summary>
        public static readonly IValueConverter BoolToBrush = new BoolToBrushConverter();

        /// <summary>
        /// Bool -> Double (可配置TrueValue/FalseValue)
        /// </summary>
        public static readonly IValueConverter BoolToDouble = new BoolToDoubleConverter();

        /// <summary>
        /// Bool -> String (可配置TrueText/FalseText)
        /// </summary>
        public static readonly IValueConverter BoolToString = new BoolToStringConverter();

        /// <summary>
        /// Bool -> Opacity (true=1.0, false=0.5)
        /// </summary>
        public static readonly IValueConverter BoolToOpacity = new BoolToOpacityConverter();

        // ========== Visibility Converters ==========

        /// <summary>
        /// String -> Visibility (null/empty=Collapsed, otherwise=Visible)
        /// </summary>
        public static readonly IValueConverter StringToVis = new StringToVisibilityConverter();

        /// <summary>
        /// Object -> Visibility (null=Collapsed, otherwise=Visible)
        /// </summary>
        public static readonly IValueConverter NullToVis = new NullToVisibilityConverter();

        /// <summary>
        /// Object -> Visibility (null=Visible, otherwise=Collapsed)
        /// </summary>
        public static readonly IValueConverter InverseNullToVis = new InverseNullToVisibilityConverter();

        // ========== Enum/Status Converters ==========

        /// <summary>
        /// Enum -> Description属性值
        /// </summary>
        public static readonly IValueConverter EnumDesc = new EnumDescriptionConverter();

        /// <summary>
        /// Status -> Color
        /// </summary>
        public static readonly IValueConverter StatusToColor = new StatusToColorConverter();

        /// <summary>
        /// ApiHealthStatus -> Color
        /// </summary>
        public static readonly IValueConverter ApiStatusToColor = new ApiHealthStatusToColorConverter();

        /// <summary>
        /// ApiHealthStatus -> Text
        /// </summary>
        public static readonly IValueConverter ApiStatusToText = new ApiHealthStatusToTextConverter();

        // ========== String Converters ==========

        /// <summary>
        /// String -> 首字符
        /// </summary>
        public static readonly IValueConverter FirstChar = new FirstCharacterConverter();

        // ========== Domain-specific Converters ==========

        /// <summary>
        /// DecocteMethod -> Visibility (特殊煎法显示控制)
        /// </summary>
        public static readonly IValueConverter DecocteMethodToVis = new DecocteMethodToVisibilityConverter();

        /// <summary>
        /// PatientCardDisplayMode -> Visibility
        /// </summary>
        public static readonly IValueConverter PatientCardModeToVis = new PatientCardDisplayModeToVisibilityConverter();
    }
}
