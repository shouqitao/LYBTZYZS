using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace LYBT.Common.Extensions {

    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static partial class StringExtensions {

        // 预编译正则表达式以提升性能
        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"[^\w\s]", RegexOptions.Compiled)]
        private static partial Regex SpecialCharsRegex();

        [GeneratedRegex(@"^[\u4e00-\u9fa5]+$", RegexOptions.Compiled)]
        private static partial Regex ChineseRegex();

        /// <summary>
        /// 判断字符串是否为空或 null
        /// </summary>
        public static bool IsNullOrEmpty(this string? value) {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 判断字符串是否为空、null或仅包含空白字符
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? value) {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 安全地去除首尾空白，null 时返回空字符串
        /// </summary>
        public static string SafeTrim(this string? value) {
            return value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 如果字符串为null或空，返回默认值
        /// </summary>
        public static string IfNullOrEmpty(this string? value, string defaultValue = "") {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// 如果字符串为null、空或仅包含空白字符，返回默认值
        /// </summary>
        public static string IfNullOrWhiteSpace(this string? value, string defaultValue = "") {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        /// <summary>
        /// 截取字符串到指定长度，超出部分用省略号表示
        /// </summary>
        public static string Truncate(this string value, int maxLength, string suffix = "...") {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength) {
                return value;
            }

            return value[..(maxLength - suffix.Length)] + suffix;
        }

        /// <summary>
        /// 移除所有空白字符
        /// </summary>
        public static string RemoveWhitespace(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            return WhitespaceRegex().Replace(value, string.Empty);
        }

        /// <summary>
        /// 移除特殊字符，只保留字母、数字和空格
        /// </summary>
        public static string RemoveSpecialChars(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            return SpecialCharsRegex().Replace(value, string.Empty);
        }

        /// <summary>
        /// 判断是否为纯中文字符串
        /// </summary>
        public static bool IsChineseOnly(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return false;
            }

            return ChineseRegex().IsMatch(value);
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        public static string ToTitleCase(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            return char.ToUpper(value[0]) + (value.Length > 1 ? value[1..].ToLower() : string.Empty);
        }

        /// <summary>
        /// 计算字符串的MD5哈希值
        /// </summary>
        public static string ToMd5(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }

            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(value);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Base64编码
        /// </summary>
        public static string ToBase64(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        public static string FromBase64(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }

            try {
                var bytes = Convert.FromBase64String(value);
                return Encoding.UTF8.GetString(bytes);
            } catch {
                return string.Empty;
            }
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        public static bool IsValidEmail(this string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            try {
                var mailAddress = new System.Net.Mail.MailAddress(value);
                return mailAddress.Address == value;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// 脱敏手机号
        /// </summary>
        public static string MaskPhoneNumber(this string phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7) {
                return phoneNumber ?? string.Empty;
            }

            return phoneNumber.Length == 11
                ? $"{phoneNumber[..3]}****{phoneNumber[7..]}"
                : $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
        }

        /// <summary>
        /// 脱敏身份证号
        /// </summary>
        public static string MaskIdNumber(this string idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 8) {
                return idNumber ?? string.Empty;
            }

            return idNumber.Length == 18
                ? $"{idNumber[..6]}********{idNumber[14..]}"
                : $"{idNumber[..3]}****{idNumber[^2..]}";
        }

        /// <summary>
        /// 安全地转换为整数
        /// </summary>
        public static int ToInt(this string value, int defaultValue = 0) {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为小数
        /// </summary>
        public static decimal ToDecimal(this string value, decimal defaultValue = 0) {
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为布尔值
        /// </summary>
        public static bool ToBool(this string value, bool defaultValue = false) {
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为DateTime
        /// </summary>
        public static DateTime? ToDateTime(this string value) {
            return DateTime.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// 检查字符串是否包含中文字符
        /// </summary>
        public static bool ContainsChinese(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return false;
            }

            return value.Any(c => c >= 0x4e00 && c <= 0x9fa5);
        }

        /// <summary>
        /// 反转字符串
        /// </summary>
        public static string Reverse(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            var chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        /// <summary>
        /// 计算字符串的字节长度（UTF-8编码）
        /// </summary>
        public static int GetByteLength(this string value) {
            if (string.IsNullOrEmpty(value)) {
                return 0;
            }

            return Encoding.UTF8.GetByteCount(value);
        }
    }
}