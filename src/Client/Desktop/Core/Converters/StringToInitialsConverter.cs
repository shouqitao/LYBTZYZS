using System.Globalization;
using System.Windows.Data;

namespace LYBT.Desktop.Core.Converters
{

    /// <summary>
    /// 字符串转换为首字母缩写转换器
    /// 提取姓名的首字母作为头像显示
    /// </summary>
    public class StringToInitialsConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string name || string.IsNullOrWhiteSpace(name))
            {
                return "N/A";
            }

            var trimmedName = name.Trim();

            // 处理中文姓名
            if (IsChinese(trimmedName))
            {
                // 中文姓名取前2个字符，或者只有1个字符时取1个
                return trimmedName.Length > 1 ? trimmedName.Substring(0, 2) : trimmedName;
            }

            // 处理英文姓名
            var parts = trimmedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                // 单个单词，取前2个字母
                return parts[0].Length > 1 ? parts[0].Substring(0, 2).ToUpper() : parts[0].ToUpper();
            }
            else
            {
                // 多个单词，取每个单词的首字母，最多2个
                var initials = string.Join(string.Empty, parts.Take(2).Select(part =>
                    part.Length > 0 ? char.ToUpper(part[0]) : ' '));
                return initials.Trim();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringToInitialsConverter does not support ConvertBack");
        }

        /// <summary>
        /// 判断字符串是否包含中文字符
        /// </summary>
        private static bool IsChinese(string text)
        {
            return text.Any(c => c >= 0x4e00 && c <= 0x9fbb);
        }
    }
}
