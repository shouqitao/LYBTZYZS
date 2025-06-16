namespace LYBT.Common.Extensions {

    public static class DateTimeExtensions {

        public static string ToStandardDateTime(this DateTime dt) {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string ToCnDate(this DateTime dt) {
            return dt.ToString("yyyy年MM月dd日");
        }
    }
}