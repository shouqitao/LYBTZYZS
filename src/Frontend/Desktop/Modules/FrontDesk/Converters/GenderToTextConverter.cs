using System;
using System.Globalization;
using System.Windows.Data;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.WPF.Client.Modules.FrontDesk.Converters
{
    /// <summary>
    /// 性别枚举转文本转换器（使用统一的共享枚举）
    /// </summary>
    public class GenderToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Gender gender)
            {
                return gender.GetDescription();
            }
            else if (value is int genderInt)
            {
                var genderEnum = (Gender)genderInt;
                return genderEnum.GetDescription();
            }
            return Gender.Unknown.GetDescription();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string genderText)
            {
                return genderText switch
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