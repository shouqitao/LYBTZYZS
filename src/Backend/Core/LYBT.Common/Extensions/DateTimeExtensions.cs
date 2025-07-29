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

        /// <summary>
        /// 转换为中文日期时间字符串
        /// </summary>
        public static string ToCnDateTime(this DateTime dt) {
            return dt.ToString("yyyy年MM月dd日 HH:mm:ss");
        }

        /// <summary>
        /// 转换为ISO 8601格式字符串
        /// </summary>
        public static string ToIso8601(this DateTime dt) {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// 转换为友好的时间显示（如"刚刚"、"5分钟前"）
        /// </summary>
        public static string ToFriendlyString(this DateTime dt) {
            var span = DateTime.Now - dt;

            if (span.TotalDays > 365) {
                return $"{(int)(span.TotalDays / 365)}年前";
            }
            if (span.TotalDays > 30) {
                return $"{(int)(span.TotalDays / 30)}个月前";
            }
            if (span.TotalDays > 1) {
                return $"{(int)span.TotalDays}天前";
            }
            if (span.TotalHours > 1) {
                return $"{(int)span.TotalHours}小时前";
            }
            if (span.TotalMinutes > 1) {
                return $"{(int)span.TotalMinutes}分钟前";
            }

            return "刚刚";
        }

        /// <summary>
        /// 获取时间段的开始时间（当天0点）
        /// </summary>
        public static DateTime StartOfDay(this DateTime dt) {
            return dt.Date;
        }

        /// <summary>
        /// 获取时间段的结束时间（当天23:59:59）
        /// </summary>
        public static DateTime EndOfDay(this DateTime dt) {
            return dt.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// 获取月初时间
        /// </summary>
        public static DateTime StartOfMonth(this DateTime dt) {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// 获取月末时间
        /// </summary>
        public static DateTime EndOfMonth(this DateTime dt) {
            return dt.StartOfMonth().AddMonths(1).AddTicks(-1);
        }

        /// <summary>
        /// 判断是否是同一天
        /// </summary>
        public static bool IsSameDay(this DateTime dt, DateTime other) {
            return dt.Date == other.Date;
        }

        /// <summary>
        /// 判断是否是工作日（周一到周五）
        /// </summary>
        public static bool IsWeekday(this DateTime dt) {
            return dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday;
        }

        /// <summary>
        /// 判断是否是周末
        /// </summary>
        public static bool IsWeekend(this DateTime dt) {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// 获取年龄
        /// </summary>
        public static int GetAge(this DateTime birthDate) {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) {
                age--;
            }
            return age;
        }
    }
}