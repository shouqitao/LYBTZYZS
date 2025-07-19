using LYBT.Common.Enums.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LYBT.UI.WPF.Converters {
    /// <summary>
    /// 角色列表转字符串转换器
    /// </summary>
    [ValueConversion(typeof(IEnumerable<UserRole>), typeof(string))]
    public class RoleListToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not IEnumerable<UserRole> roles)
                return "无角色";

            var roleDescriptions = roles.Select(role => {
                var field = role.GetType().GetField(role.ToString());
                var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
                return attribute?.Description ?? role.ToString();
            });

            var result = string.Join(", ", roleDescriptions);
            return string.IsNullOrEmpty(result) ? "无角色" : result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException("RoleListToStringConverter does not support ConvertBack");
        }
    }
}
