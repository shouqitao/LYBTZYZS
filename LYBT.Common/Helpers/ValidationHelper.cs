using System.Text.RegularExpressions;

namespace LYBT.Common.Helpers {
    /// <summary>
    /// 通用输入字段验证工具类（用于前后端校验）
    /// </summary>
    public static class ValidationHelper {
        /// <summary>
        /// 是否为合法的手机号（11位，1开头）
        /// </summary>
        public static bool IsValidPhone(string? phone) {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // 国内手机号正则：1开头+10位数字
            return Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
        }

        /// <summary>
        /// 是否为合法身份证号码（15位或18位，支持 X/x）
        /// </summary>
        public static bool IsValidIDNumber(string? idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            return Regex.IsMatch(idNumber, @"^\d{15}$|^\d{17}[\dXx]$");
        }

        /// <summary>
        /// 是否为非空字符串（去除空格后长度大于0）
        /// </summary>
        public static bool IsNotEmpty(string? text) {
            return !string.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// 通用多字段非空校验（全为 true 才返回 true）
        /// </summary>
        public static bool AreRequiredFieldsValid(params string?[] fields) {
            return fields.All(IsNotEmpty);
        }
    }
}
