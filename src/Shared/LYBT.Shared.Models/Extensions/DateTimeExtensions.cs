namespace LYBT.Shared.Models.Extensions {

    /// <summary>
    /// DateTime 扩展方法 - 前后端共享
    /// 提供日期时间的常用格式化和操作功能
    /// </summary>
    public static class DateTimeExtensions {

        /// <summary>
        /// 转换为标准日期时间字符串 (yyyy-MM-dd HH:mm:ss)
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <returns>标准格式的日期时间字符串</returns>
        public static string ToStandardDateTime(this DateTime dt) {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 转换为中文日期字符串 (yyyy年MM月dd日)
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <returns>中文格式的日期字符串</returns>
        public static string ToCnDate(this DateTime dt) {
            return dt.ToString("yyyy年MM月dd日");
        }

        /// <summary>
        /// 转换为中文日期时间字符串 (yyyy年MM月dd日 HH:mm:ss)
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <returns>中文格式的日期时间字符串</returns>
        public static string ToCnDateTime(this DateTime dt) {
            return dt.ToString("yyyy年MM月dd日 HH:mm:ss");
        }

        /// <summary>
        /// 转换为ISO 8601格式字符串 (yyyy-MM-ddTHH:mm:ss.fffZ)
        /// </summary>
        /// <param name="dt">要格式化的日期时间</param>
        /// <returns>ISO 8601格式的日期时间字符串</returns>
        public static string ToIso8601(this DateTime dt) {
            return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// 转换为友好的时间显示（如"刚刚"、"5分钟前"、"3天前"）
        /// 适用于显示相对时间，提升用户体验
        /// </summary>
        /// <param name="dt">要转换的日期时间</param>
        /// <returns>友好格式的时间字符串</returns>
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
        /// <param name="dt">输入的日期时间</param>
        /// <returns>当天的开始时间（00:00:00）</returns>
        public static DateTime StartOfDay(this DateTime dt) {
            return dt.Date;
        }

        /// <summary>
        /// 获取时间段的结束时间（当天23:59:59.9999999）
        /// </summary>
        /// <param name="dt">输入的日期时间</param>
        /// <returns>当天的结束时间（23:59:59.9999999）</returns>
        public static DateTime EndOfDay(this DateTime dt) {
            return dt.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// 获取月初时间（本月第一天的00:00:00）
        /// </summary>
        /// <param name="dt">输入的日期时间</param>
        /// <returns>本月的第一天</returns>
        public static DateTime StartOfMonth(this DateTime dt) {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        /// <summary>
        /// 获取月末时间（本月最后一天的23:59:59.9999999）
        /// </summary>
        /// <param name="dt">输入的日期时间</param>
        /// <returns>本月的最后一天</returns>
        public static DateTime EndOfMonth(this DateTime dt) {
            return dt.StartOfMonth().AddMonths(1).AddTicks(-1);
        }

        /// <summary>
        /// 判断是否是同一天
        /// </summary>
        /// <param name="dt">要比较的第一个日期时间</param>
        /// <param name="other">要比较的第二个日期时间</param>
        /// <returns>如果是同一天返回true，否则返回false</returns>
        public static bool IsSameDay(this DateTime dt, DateTime other) {
            return dt.Date == other.Date;
        }

        /// <summary>
        /// 判断是否是工作日（周一到周五）
        /// </summary>
        /// <param name="dt">要判断的日期时间</param>
        /// <returns>如果是工作日返回true，否则返回false</returns>
        public static bool IsWeekday(this DateTime dt) {
            return dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday;
        }

        /// <summary>
        /// 判断是否是周末（周六或周日）
        /// </summary>
        /// <param name="dt">要判断的日期时间</param>
        /// <returns>如果是周末返回true，否则返回false</returns>
        public static bool IsWeekend(this DateTime dt) {
            return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// 根据出生日期计算年龄
        /// 考虑了当前年份中是否已过生日的情况
        /// </summary>
        /// <param name="birthDate">出生日期</param>
        /// <returns>当前年龄</returns>
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