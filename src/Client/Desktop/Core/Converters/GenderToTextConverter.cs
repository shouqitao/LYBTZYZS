using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Converters
{
    /// <summary>
    /// 性别枚举转文本转换器
    /// </summary>
    public class GenderToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Gender gender)
            {
                return gender switch
                {
                    Gender.Male => "男",
                    Gender.Female => "女",
                    _ => "未知"
                };
            }

            if (value is int genderInt)
            {
                return genderInt switch
                {
                    1 => "男",
                    2 => "女",
                    _ => "未知"
                };
            }

            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text switch
                {
                    "男" => Gender.Male,
                    "女" => Gender.Female,
                    _ => Gender.Unknown
                };
            }
            return Gender.Unknown;
        }
    }
}