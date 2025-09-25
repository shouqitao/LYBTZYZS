using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的Boolean转换器 - 第2阶段架构重构
    /// 合并12个Boolean相关转换器的功能
    /// 通过参数控制转换目标类型
    /// </summary>
    [ValueConversion(typeof(bool), typeof(object))]
    public class UnifiedBooleanConverter : IValueConverter
    {
        /// <summary>
        /// True时的值
        /// </summary>
        public object? TrueValue { get; set; }
        
        /// <summary>
        /// False时的值
        /// </summary>
        public object? FalseValue { get; set; }
        
        /// <summary>
        /// 是否反转布尔值
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// 转换类型（通过参数指定）
        /// </summary>
        public enum ConversionType
        {
            Default,        // 使用TrueValue/FalseValue
            Visibility,     // 转换为Visibility
            Brush,          // 转换为Brush
            String,         // 转换为字符串
            Color,          // 转换为Color
            Icon,           // 转换为图标字符串
            Status          // 转换为状态字符串
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            
            // 处理输入值
            if (value is bool b)
            {
                boolValue = Invert ? !b : b;
            }
            else if (value != null)
            {
                // 尝试转换其他类型为bool
                boolValue = System.Convert.ToBoolean(value);
                if (Invert) boolValue = !boolValue;
            }

            // 解析参数
            var conversionType = ParseConversionType(parameter);
            
            // 根据转换类型返回相应值
            return conversionType switch
            {
                ConversionType.Visibility => ConvertToVisibility(boolValue),
                ConversionType.Brush => ConvertToBrush(boolValue, parameter),
                ConversionType.String => ConvertToString(boolValue, parameter),
                ConversionType.Color => ConvertToColor(boolValue, parameter),
                ConversionType.Icon => ConvertToIcon(boolValue, parameter),
                ConversionType.Status => ConvertToStatus(boolValue, parameter),
                _ => ConvertDefault(boolValue)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ParseConversionType(parameter) == ConversionType.Visibility)
            {
                if (value is Visibility visibility)
                {
                    var result = visibility == Visibility.Visible;
                    return Invert ? !result : result;
                }
            }
            
            if (value?.Equals(TrueValue) == true)
                return !Invert;
            if (value?.Equals(FalseValue) == true)
                return Invert;
                
            throw new NotSupportedException($"UnifiedBooleanConverter不支持从{value?.GetType()}反向转换");
        }

        #region 私有转换方法

        private ConversionType ParseConversionType(object? parameter)
        {
            if (parameter == null) return ConversionType.Default;
            
            var paramStr = parameter.ToString() ?? "";
            var parts = paramStr.Split('|');
            
            if (parts.Length > 0)
            {
                if (Enum.TryParse<ConversionType>(parts[0], true, out var type))
                {
                    return type;
                }
            }
            
            return ConversionType.Default;
        }

        private object ConvertToVisibility(bool value)
        {
            return value ? Visibility.Visible : Visibility.Collapsed;
        }

        private object ConvertToBrush(bool value, object? parameter)
        {
            // 参数格式: "Brush|#TrueColor|#FalseColor"
            if (parameter != null)
            {
                var parts = parameter.ToString()?.Split('|');
                if (parts?.Length >= 3)
                {
                    var colorStr = value ? parts[1] : parts[2];
                    return CreateBrushFromString(colorStr);
                }
            }
            
            // 默认：绿色(true) / 灰色(false)
            return value 
                ? new SolidColorBrush(Colors.Green) 
                : new SolidColorBrush(Colors.Gray);
        }

        private object ConvertToString(bool value, object? parameter)
        {
            // 参数格式: "String|TrueText|FalseText"
            if (parameter != null)
            {
                var parts = parameter.ToString()?.Split('|');
                if (parts?.Length >= 3)
                {
                    return value ? parts[1] : parts[2];
                }
            }
            
            return value ? "是" : "否";
        }

        private object ConvertToColor(bool value, object? parameter)
        {
            // 参数格式: "Color|#TrueColor|#FalseColor"
            if (parameter != null)
            {
                var parts = parameter.ToString()?.Split('|');
                if (parts?.Length >= 3)
                {
                    var colorStr = value ? parts[1] : parts[2];
                    return ParseColor(colorStr);
                }
            }
            
            return value ? Colors.Green : Colors.Gray;
        }

        private object ConvertToIcon(bool value, object? parameter)
        {
            // 参数格式: "Icon|TrueIcon|FalseIcon"
            if (parameter != null)
            {
                var parts = parameter.ToString()?.Split('|');
                if (parts?.Length >= 3)
                {
                    return value ? parts[1] : parts[2];
                }
            }
            
            // 默认图标（使用Material Design Icons）
            return value ? "CheckCircle" : "Cancel";
        }

        private object ConvertToStatus(bool value, object? parameter)
        {
            // 参数格式: "Status|TrueStatus|FalseStatus"
            if (parameter != null)
            {
                var parts = parameter.ToString()?.Split('|');
                if (parts?.Length >= 3)
                {
                    return value ? parts[1] : parts[2];
                }
            }
            
            return value ? "在线" : "离线";
        }

        private object ConvertDefault(bool value)
        {
            if (TrueValue != null && FalseValue != null)
            {
                return value ? TrueValue : FalseValue;
            }
            
            return value;
        }

        private Brush CreateBrushFromString(string colorStr)
        {
            try
            {
                var color = ParseColor(colorStr);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        private Color ParseColor(string colorStr)
        {
            if (string.IsNullOrWhiteSpace(colorStr))
                return Colors.Gray;
                
            // 移除#前缀
            colorStr = colorStr.TrimStart('#');
            
            if (colorStr.Length == 6 &&
                byte.TryParse(colorStr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                byte.TryParse(colorStr.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                byte.TryParse(colorStr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
            {
                return Color.FromRgb(r, g, b);
            }
            
            return Colors.Gray;
        }

        #endregion
    }
}