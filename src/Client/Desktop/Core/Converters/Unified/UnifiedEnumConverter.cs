using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters.Unified
{
    /// <summary>
    /// 统一的枚举转换器 - 第2阶段架构重构
    /// 合并所有枚举相关转换器功能
    /// </summary>
    [ValueConversion(typeof(Enum), typeof(object))]
    public class UnifiedEnumConverter : IValueConverter
    {
        /// <summary>
        /// 转换模式
        /// </summary>
        public EnumConversionMode Mode { get; set; } = EnumConversionMode.Description;
        
        public enum EnumConversionMode
        {
            Description,    // 转换为Description特性
            DisplayName,    // 转换为DisplayName特性
            Name,          // 转换为枚举名称
            Value,         // 转换为枚举值
            Localized      // 转换为本地化字符串
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
                
            if (!(value is Enum enumValue))
            {
                if (value.GetType().IsEnum)
                {
                    enumValue = (Enum)value;
                }
                else
                {
                    return value.ToString() ?? string.Empty;
                }
            }
            
            // 通过参数覆盖模式
            if (parameter != null && Enum.TryParse<EnumConversionMode>(parameter.ToString(), out var mode))
            {
                return ConvertEnum(enumValue, mode);
            }
            
            return ConvertEnum(enumValue, Mode);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !targetType.IsEnum)
                throw new ArgumentException("UnifiedEnumConverter反向转换需要枚举类型");
                
            var strValue = value.ToString();
            if (string.IsNullOrEmpty(strValue))
                return Activator.CreateInstance(targetType);
                
            // 尝试直接解析
            if (Enum.TryParse(targetType, strValue, true, out var result))
                return result;
                
            // 尝试通过Description匹配
            foreach (var field in targetType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var description = GetDescription(field);
                if (description == strValue)
                {
                    return field.GetValue(null);
                }
            }
            
            throw new ArgumentException($"无法将'{strValue}'转换为{targetType.Name}");
        }

        private string ConvertEnum(Enum enumValue, EnumConversionMode mode)
        {
            return mode switch
            {
                EnumConversionMode.Description => GetDescription(enumValue),
                EnumConversionMode.DisplayName => GetDisplayName(enumValue),
                EnumConversionMode.Name => enumValue.ToString(),
                EnumConversionMode.Value => System.Convert.ToInt32(enumValue).ToString(),
                EnumConversionMode.Localized => GetLocalizedString(enumValue),
                _ => enumValue.ToString()
            };
        }

        private string GetDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field == null) return enumValue.ToString();
            
            return GetDescription(field);
        }

        private string GetDescription(FieldInfo field)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? field.Name;
        }

        private string GetDisplayName(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field == null) return enumValue.ToString();
            
            var attribute = field.GetCustomAttribute<DisplayNameAttribute>();
            return attribute?.DisplayName ?? GetDescription(field);
        }

        private string GetLocalizedString(Enum enumValue)
        {
            // 这里可以集成资源管理器进行本地化
            // 暂时返回Description
            return GetDescription(enumValue);
        }
    }
}