namespace LYBT.Common.Extensions {

    /// <summary>
    /// DateTime 扩展方法
    /// </summary>
    public static class DateTimeExtensions {

        /// <summary>
        /// 转换为标准日期时间字符串
        /// </summary>
        public static string ToStandardDateTime(this DateTime dt) {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 转换为中文日期字符串
        /// </summary>
        public static string ToCnDate(this DateTime dt) {
            return dt.ToString("yyyy年MM月dd日");
        }
    }
}