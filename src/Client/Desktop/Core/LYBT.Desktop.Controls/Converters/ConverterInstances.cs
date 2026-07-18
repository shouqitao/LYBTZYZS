using System.Windows.Data;

namespace LYBT.Desktop.Controls.Converters
{
    /// <summary>
    /// 转换器静态实例提供者
    ///
    /// 解决WPF资源架构问题:
    /// - Binding.Converter 不是 DependencyProperty，必须使用 StaticResource
    /// - StaticResource 要求资源在 XAML 解析时已存在
    /// - 当控件被加载到 ContentPresenter（如 MasterDetailLayout）时，资源查找路径可能断裂
    ///
    /// 解决方案：使用 x:Static 引用静态实例，完全绕过资源字典查找机制
    ///
    /// 使用方式:
    /// <code>
    /// xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters;assembly=LYBT.Desktop.Infrastructure"
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
        public static readonly IValueConverter BoolToVis = new BooleanToVisibilityConverter();
        public static readonly IValueConverter BoolToInt = new BoolToIntConverter();
        public static readonly IValueConverter BoolToBrush = new BoolToBrushConverter();
        public static readonly IValueConverter BoolToColor = new BoolToColorConverter();
        public static readonly IValueConverter InverseBoolToVis = new InverseBooleanToVisibilityConverter();
        public static readonly IValueConverter InverseBool = new InverseBooleanConverter();
        public static readonly IValueConverter StringToVis = new StringToVisibilityConverter();
        public static readonly IValueConverter NullToVis = new NullToVisibilityConverter();
        public static readonly IValueConverter NotNullToVis = new NullToVisibilityConverter();
        public static readonly IValueConverter InverseNullToVis = new InverseNullToVisibilityConverter();
        public static readonly IValueConverter EnumDesc = new EnumDescriptionConverter();
        public static readonly IValueConverter FirstChar = new FirstCharacterConverter();
        public static readonly IValueConverter DecocteMethodToVis = new DecocteMethodToVisibilityConverter();
    }
}
