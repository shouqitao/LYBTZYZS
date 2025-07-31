using System;
using System.Globalization;
using System.Windows.Data;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.Converters
{
    /// <summary>
    /// 性别值转换器
    /// </summary>
    public class GenderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int gender)
            {
                return gender switch
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
            if (value is string genderText)
            {
                return genderText switch
                {
                    "男" => 1,
                    "女" => 2,
                    _ => 0
                };
            }
            return 0;
        }
    }
}