using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters
{
    /// <summary>
    /// 枚举描述转换器
    /// 将枚举值转换为其 Description 特性标注的文本
    /// 用于在UI中显示枚举的中文名称
    /// </summary>
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            // 获取枚举值的类型
            Type enumType = value.GetType();
            if (!enumType.IsEnum)
                return value.ToString() ?? string.Empty;

            // 获取枚举字段
            string? enumName = Enum.GetName(enumType, value);
            if (string.IsNullOrEmpty(enumName))
                return value.ToString() ?? string.Empty;

            FieldInfo? fieldInfo = enumType.GetField(enumName);
            if (fieldInfo == null)
                return enumName;

            // 尝试获取 Description 特性
            var descriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description;
            }

            // 如果没有 Description 特性，返回枚举名称
            return enumName;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 不支持反向转换
            return DependencyProperty.UnsetValue;
        }
    }
}
