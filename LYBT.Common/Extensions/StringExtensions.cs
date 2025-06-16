namespace LYBT.Common.Extensions {

    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions {

        /// <summary>
        /// 判断字符串是否为空或 null
        /// </summary>
        public static bool IsNullOrEmpty(this string? value) {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 安全地去除首尾空白，null 时返回空字符串
        /// </summary>
        public static string SafeTrim(this string? value) {
            return value?.Trim() ?? string.Empty;
        }
    }
}