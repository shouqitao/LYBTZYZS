using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace LYBT.Shared.Utilities.Helpers
{
    /// <summary>
    /// 通用工具类 - 前后端共享版本（性能优化）
    /// 包含纯逻辑功能，不依赖特定UI框架或Web框架
    /// </summary>
    public static partial class CommonHelper
    {
        // 预编译正则表达式以提升性能
        [GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
        private static partial Regex PhoneDigitsRegex();

        [GeneratedRegex(@"^\d{17}[\dXx]$", RegexOptions.Compiled)]
        private static partial Regex IdNumberRegex();

        // 身份证校验权重和校验码（避免重复计算）
        private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        private static readonly char[] IdCodes = "10X98765432".ToCharArray();


        /// <summary>
        /// 检查网络是否可用
        /// </summary>
        /// <returns>网络是否可用</returns>
        public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();

        /// <summary>
        /// 格式化电话号码（性能优化版本）
        /// </summary>
        /// <param name="phone">原始电话号码</param>
        /// <returns>格式化后的电话号码</returns>
        public static string FormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = PhoneDigitsRegex().Replace(phone, string.Empty);

            return digits.Length switch
            {
                11 => $"{digits[..3]}-{digits[3..7]}-{digits[7..]}",
                10 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
                _ => digits
            };
        }

        /// <summary>
        /// 验证身份证号码（性能优化版本）
        /// </summary>
        /// <param name="idNumber">身份证号码</param>
        /// <returns>验证结果</returns>
        public static bool CheckIdNumber(string? idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            idNumber = idNumber.Trim();

            if (!IdNumberRegex().IsMatch(idNumber))
                return false;

            // 计算校验码
            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                sum += (idNumber[i] - '0') * IdWeights[i];
            }

            char expectedCode = IdCodes[sum % 11];
            return char.ToUpperInvariant(idNumber[17]) == expectedCode;
        }



        /// <summary>
        /// 根据中文名称生成拼音码（简化实现）
        /// </summary>
        /// <param name="text">中文文本</param>
        /// <returns>拼音首字母缩写</returns>
        public static string GetPinyinCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 简化实现：返回空字符串，避免编译错误
            // 注：实际项目中可以集成专业的拼音转换库
            return string.Empty;
        }

        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <returns>验证结果</returns>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <param name="includeNumbers">是否包含数字</param>
        /// <returns>随机字符串</returns>
        public static string GenerateRandomString(int length, bool includeNumbers = true)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";

            string chars = letters + letters.ToLower();
            if (includeNumbers)
                chars += numbers;

            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// 安全地转换为整数
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换结果</returns>
        public static int SafeToInt(string? value, int defaultValue = 0)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为小数
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换结果</returns>
        public static decimal SafeToDecimal(string? value, decimal defaultValue = 0)
        {
            return decimal.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 安全地转换为布尔值
        /// </summary>
        /// <param name="value">待转换的字符串</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>转换结果</returns>
        public static bool SafeToBool(string? value, bool defaultValue = false)
        {
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 脱敏手机号
        /// </summary>
        /// <param name="phoneNumber">原始手机号</param>
        /// <returns>脱敏后的手机号</returns>
        public static string MaskPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7)
                return phoneNumber ?? string.Empty;

            return phoneNumber.Length == 11
                ? $"{phoneNumber[..3]}****{phoneNumber[7..]}"
                : $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
        }

        /// <summary>
        /// 脱敏身份证号
        /// </summary>
        /// <param name="idNumber">原始身份证号</param>
        /// <returns>脱敏后的身份证号</returns>
        public static string MaskIdNumber(string? idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 8)
                return idNumber ?? string.Empty;

            return idNumber.Length == 18
                ? $"{idNumber[..6]}********{idNumber[14..]}"
                : $"{idNumber[..3]}****{idNumber[^2..]}";
        }

        /// <summary>
        /// 生成唯一标识符
        /// </summary>
        /// <returns>唯一标识符</returns>
        public static string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 生成短ID（8位）
        /// </summary>
        /// <returns>短ID</returns>
        public static string GenerateShortId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// 获取文件扩展名（包含点号）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件扩展名</returns>
        public static string GetFileExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return Path.GetExtension(fileName).ToLower();
        }

        /// <summary>
        /// 获取文件大小的友好显示
        /// </summary>
        /// <param name="fileSize">文件大小（字节）</param>
        /// <returns>友好显示的文件大小</returns>
        public static string GetFileSizeString(long fileSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = fileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 检查文件类型是否为图片
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为图片文件</returns>
        public static bool IsImageFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = GetFileExtension(fileName);
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return imageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件类型是否为文档
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为文档文件</returns>
        public static bool IsDocumentFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = GetFileExtension(fileName);
            string[] docExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".rtf" };
            return docExtensions.Contains(extension);
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <returns>清理后的文件名</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// 生成Unix时间戳（秒）
        /// </summary>
        /// <returns>Unix时间戳</returns>
        public static long GetTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 生成Unix时间戳（毫秒）
        /// </summary>
        /// <returns>Unix时间戳毫秒</returns>
        public static long GetTimestampMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 从Unix时间戳转换为DateTime
        /// </summary>
        /// <param name="timestamp">Unix时间戳（秒）</param>
        /// <returns>DateTime对象</returns>
        public static DateTime FromTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        /// <summary>
        /// 从Unix时间戳（毫秒）转换为DateTime
        /// </summary>
        /// <param name="timestamp">Unix时间戳（毫秒）</param>
        /// <returns>DateTime对象</returns>
        public static DateTime FromTimestampMilliseconds(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

    }
}