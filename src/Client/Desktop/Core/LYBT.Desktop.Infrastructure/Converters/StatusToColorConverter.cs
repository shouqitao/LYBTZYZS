using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 统一的状态到颜色转换器
    /// 合并了原有的：
    /// - StateToColorConverter
    /// - StatusToBackgroundConverter
    /// - StatusToBrushConverter
    /// 支持多种状态枚举类型和输出格式
    /// </summary>
    public class StatusToColorConverter : IValueConverter, IMultiValueConverter
    {
        // 默认颜色定义
        private static readonly Dictionary<string, Color> StatusColors = new()
        {
            // 通用状态
            { "Active", Color.FromRgb(76, 175, 80) },      // 绿色
            { "Inactive", Color.FromRgb(158, 158, 158) },  // 灰色
            { "Pending", Color.FromRgb(255, 193, 7) },     // 黄色
            { "Completed", Color.FromRgb(33, 150, 243) },  // 蓝色
            { "Failed", Color.FromRgb(244, 67, 54) },      // 红色
            { "Cancelled", Color.FromRgb(117, 117, 117) }, // 深灰
            { "InProgress", Color.FromRgb(3, 169, 244) },  // 浅蓝
            
            // 医疗相关状态
            { "Waiting", Color.FromRgb(255, 152, 0) },     // 橙色
            { "InConsultation", Color.FromRgb(76, 175, 80) }, // 绿色
            { "Diagnosed", Color.FromRgb(33, 150, 243) },  // 蓝色
            { "Prescribed", Color.FromRgb(156, 39, 176) }, // 紫色
            
            // 处方状态
            { "Draft", Color.FromRgb(158, 158, 158) },     // 灰色
            { "Submitted", Color.FromRgb(3, 169, 244) },   // 浅蓝
            { "Dispensed", Color.FromRgb(76, 175, 80) },   // 绿色
            { "Paid", Color.FromRgb(0, 150, 136) },        // 青色
            
            // 用户状态
            { "Online", Color.FromRgb(76, 175, 80) },      // 绿色
            { "Offline", Color.FromRgb(158, 158, 158) },   // 灰色
            { "Away", Color.FromRgb(255, 193, 7) },        // 黄色
            { "Busy", Color.FromRgb(244, 67, 54) }         // 红色
        };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;

            // 获取状态名称
            string statusName = value switch
            {
                Enum e => e.ToString(),
                string s => s,
                bool b => b ? "Active" : "Inactive",
                _ => value.ToString() ?? string.Empty
            };

            // 获取对应颜色
            if (!StatusColors.TryGetValue(statusName, out var color))
            {
                color = Color.FromRgb(158, 158, 158); // 默认灰色
            }

            // 根据参数或目标类型返回不同格式
            var outputFormat = parameter as string ?? DetermineOutputFormat(targetType);

            return outputFormat?.ToLowerInvariant() switch
            {
                "color" => color,
                "brush" or "solidcolorbrush" => new SolidColorBrush(color),
                "hex" => $"#{color.R:X2}{color.G:X2}{color.B:X2}",
                "rgb" => $"rgb({color.R},{color.G},{color.B})",
                "gradient" => CreateGradientBrush(color),
                _ => new SolidColorBrush(color)
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return DependencyProperty.UnsetValue;

            // 支持多值输入：第一个是状态，第二个可以是输出格式
            var outputFormat = values.Length > 1 && values[1] is string fmt ? fmt : parameter as string;
            return Convert(values[0], targetType, outputFormat, culture);
        }

        public object?[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string DetermineOutputFormat(Type targetType)
        {
            if (targetType == typeof(Color))
                return "color";
            if (targetType == typeof(Brush))
                return "brush";
            if (targetType == typeof(string))
                return "hex";

            return "brush";
        }

        private static LinearGradientBrush CreateGradientBrush(Color baseColor)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };

            brush.GradientStops.Add(new GradientStop(baseColor, 0.0));
            brush.GradientStops.Add(new GradientStop(
                Color.FromArgb(baseColor.A,
                    (byte)(baseColor.R * 0.8),
                    (byte)(baseColor.G * 0.8),
                    (byte)(baseColor.B * 0.8)),
                1.0));

            return brush;
        }
    }
}
