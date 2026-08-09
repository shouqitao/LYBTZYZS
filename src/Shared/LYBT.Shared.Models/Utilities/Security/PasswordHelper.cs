using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Utilities.Security
{
    // OpenSpec: unify-enums-to-shared - PasswordStrength已迁移到LYBT.Shared.Models.Enums.SecurityEnums.cs

    /// <summary>
    /// 纯密码工具类（生成/策略）——哈希与验证统一走 Identity UserManager（PBKDF2），本类不提供哈希方法
    /// </summary>
    // TODO: 超大类型，建议拆分（详见 docs/compose/reports/code-review-duplicates.md 🟡5）
    public static class PasswordHelper
    {
        #region 配置常量

        /// <summary>
        /// 随机字节长度
        /// </summary>
        private const int RandomByteLength = 32;

        /// <summary>
        /// 常见弱密码列表（从PasswordLegacyHelper迁移）
        /// </summary>
        private static readonly HashSet<string> WeakPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "admin", "123456789", "qwerty",
            "abc123", "password123", "admin123", "123123", "111111",
            "666666", "888888", "1234567890", "root", "user",
            "guest", "test", "welcome", "letmein", "monkey",
            "dragon", "master", "123abc"
        };

        #endregion

        #region 密码强度验证功能

        /// <summary>
        /// 检查密码强度（从PasswordLegacyHelper迁移）
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>密码强度等级</returns>
        public static PasswordStrength CheckPasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return PasswordStrength.Weak;

            var score = 0;

            // 长度评分 (最多20分)
            score += Math.Min(password.Length * 2, 20);

            // 字符类型评分 (每种类型10分)
            if (password.Any(char.IsLower)) score += 10;
            if (password.Any(char.IsUpper)) score += 10;
            if (password.Any(char.IsDigit)) score += 10;
            if (!Regex.IsMatch(password, "[a-zA-Z0-9]")) score += 10;

            // 长度奖励
            if (password.Length >= 12) score += 10;
            if (password.Length >= 16) score += 10;

            // 弱密码惩罚
            if (IsCommonPassword(password)) score -= 20;

            // 转换为强度等级
            return score switch
            {
                >= 60 => PasswordStrength.VeryStrong,
                >= 50 => PasswordStrength.Strong,
                >= 35 => PasswordStrength.Good,
                >= 20 => PasswordStrength.Fair,
                _ => PasswordStrength.Weak
            };
        }

        /// <summary>
        /// 检查是否为常见弱密码（从PasswordLegacyHelper迁移）
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>是否为弱密码</returns>
        public static bool IsCommonPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && WeakPasswords.Contains(password);
        }

        /// <summary>
        /// 生成安全的随机密码（从PasswordLegacyHelper迁移并增强）
        /// 使用RandomNumberGenerator确保密码学安全
        /// </summary>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword()
            => GenerateSecurePassword(12, true, true, true, true);

        /// <summary>
        /// 生成安全的随机密码（从PasswordLegacyHelper迁移并增强）
        /// 使用RandomNumberGenerator确保密码学安全
        /// </summary>
        /// <param name="length">密码长度（默认20，最小12）</param>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword(int length = 20)
            => GenerateSecurePassword(length, true, true, true, true);

        private static int GetRandomInt(int maxValue) => RandomNumberGenerator.GetInt32(maxValue);

        private static void Shuffle(Span<char> array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = GetRandomInt(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// 生成安全的随机密码（从PasswordLegacyHelper迁移并增强）
        /// </summary>
        /// <param name="length">密码长度（默认12）</param>
        /// <param name="includeUppercase">包含大写字母</param>
        /// <param name="includeLowercase">包含小写字母</param>
        /// <param name="includeDigits">包含数字</param>
        /// <param name="includeSpecialChars">包含特殊字符</param>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword(
            int length,
            bool includeUppercase = true,
            bool includeLowercase = true,
            bool includeDigits = true,
            bool includeSpecialChars = true)
        {
            if (length < 4)
                throw new ArgumentException("密码长度至少为4位");

            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*";

            var chars = new StringBuilder();
            var password = new StringBuilder();

            // 构建字符集并确保每种类型至少出现一次
            if (includeLowercase)
            {
                chars.Append(lowercase);
                password.Append(lowercase[GetRandomInt(lowercase.Length)]);
            }
            if (includeUppercase)
            {
                chars.Append(uppercase);
                password.Append(uppercase[GetRandomInt(uppercase.Length)]);
            }
            if (includeDigits)
            {
                chars.Append(digits);
                password.Append(digits[GetRandomInt(digits.Length)]);
            }
            if (includeSpecialChars)
            {
                chars.Append(specialChars);
                password.Append(specialChars[GetRandomInt(specialChars.Length)]);
            }

            if (chars.Length == 0)
                throw new ArgumentException("至少要包含一种字符类型");

            // 填充剩余长度
            var allChars = chars.ToString();
            while (password.Length < length)
            {
                password.Append(allChars[GetRandomInt(allChars.Length)]);
            }

            // 简单打乱字符顺序
            var result = password.ToString().ToCharArray();
            Shuffle(result);

            return new string(result);
        }

        /// <summary>
        /// 生成安全的随机密码（从PasswordLegacyHelper迁移并增强）
        /// </summary>
        /// <param name="includeUppercase">包含大写字母</param>
        /// <param name="includeLowercase">包含小写字母</param>
        /// <param name="includeDigits">包含数字</param>
        /// <param name="includeSpecialChars">包含特殊字符</param>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword(
            bool includeUppercase,
            bool includeLowercase,
            bool includeDigits,
            bool includeSpecialChars)
            => GenerateSecurePassword(12, includeUppercase, includeLowercase, includeDigits, includeSpecialChars);

        #endregion

        #region 支持类型

        /// <summary>
        /// 密码验证结果（从PasswordLegacyHelper迁移）
        /// </summary>
        public class PasswordValidationResult
        {
            /// <summary>
            /// 是否通过验证
            /// </summary>
            public bool IsValid { get; set; }

            /// <summary>
            /// 密码强度
            /// </summary>
            public PasswordStrength Strength { get; set; }

            /// <summary>
            /// 错误信息列表
            /// </summary>
            public List<string> Errors { get; set; } = [];

            /// <summary>
            /// 建议信息
            /// </summary>
            public string Suggestions { get; set; } = string.Empty;
        }

        #endregion
    }
}
