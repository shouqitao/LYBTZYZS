using System.Text.RegularExpressions;

namespace LYBT.Shared.Utilities.Security
{
    /// <summary>
    /// 密码策略验证器 - 企业级密码复杂度策略实现
    /// </summary>
    public static partial class PasswordPolicyValidator
    {
        /// <summary>
        /// 密码策略配置
        /// </summary>
        public static class Policy
        {
            /// <summary>
            /// 最小长度
            /// </summary>
            public const int MinLength = 8;

            /// <summary>
            /// 最大长度
            /// </summary>
            public const int MaxLength = 128;

            /// <summary>
            /// 要求大写字母
            /// </summary>
            public const bool RequireUppercase = true;

            /// <summary>
            /// 要求小写字母
            /// </summary>
            public const bool RequireLowercase = true;

            /// <summary>
            /// 要求数字
            /// </summary>
            public const bool RequireDigit = true;

            /// <summary>
            /// 要求特殊字符
            /// </summary>
            public const bool RequireSpecialChar = true;

            /// <summary>
            /// 特殊字符集
            /// </summary>
            public const string SpecialCharacters = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

            /// <summary>
            /// 密码历史记录数量（防止重用）
            /// </summary>
            public const int PasswordHistoryCount = 5;

            /// <summary>
            /// 密码过期天数
            /// </summary>
            public const int PasswordExpirationDays = 90;
        }

        #region 正则表达式

        [GeneratedRegex(@"[A-Z]")]
        private static partial Regex HasUppercaseRegex();

        [GeneratedRegex(@"[a-z]")]
        private static partial Regex HasLowercaseRegex();

        [GeneratedRegex(@"\d")]
        private static partial Regex HasDigitRegex();

        [GeneratedRegex(@"[!@#$%^&*()\-_+=\[\]{}|;':"",./<>?]")]
        private static partial Regex HasSpecialCharRegex();

        [GeneratedRegex(@"(.)\1{2,}")]
        private static partial Regex HasRepeatingCharactersRegex();

        [GeneratedRegex(@"(012|123|234|345|456|567|678|789|890|098|987|876|765|654|543|432|321|210)")]
        private static partial Regex HasSequentialNumbersRegex();

        [GeneratedRegex(@"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", RegexOptions.IgnoreCase)]
        private static partial Regex HasSequentialLettersRegex();

        #endregion

        /// <summary>
        /// 验证密码复杂度
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="errors">错误消息列表</param>
        /// <returns>是否通过验证</returns>
        public static bool Validate(string password, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("密码不能为空");
                return false;
            }

            // 长度检查
            if (password.Length < Policy.MinLength)
            {
                errors.Add($"密码长度不能少于 {Policy.MinLength} 位");
            }

            if (password.Length > Policy.MaxLength)
            {
                errors.Add($"密码长度不能超过 {Policy.MaxLength} 位");
            }

            // 复杂度检查
            if (Policy.RequireUppercase && !HasUppercaseRegex().IsMatch(password))
            {
                errors.Add("密码必须包含至少一个大写字母");
            }

            if (Policy.RequireLowercase && !HasLowercaseRegex().IsMatch(password))
            {
                errors.Add("密码必须包含至少一个小写字母");
            }

            if (Policy.RequireDigit && !HasDigitRegex().IsMatch(password))
            {
                errors.Add("密码必须包含至少一个数字");
            }

            if (Policy.RequireSpecialChar && !HasSpecialCharRegex().IsMatch(password))
            {
                errors.Add($"密码必须包含至少一个特殊字符 ({Policy.SpecialCharacters})");
            }

            // 安全性检查
            if (HasRepeatingCharactersRegex().IsMatch(password))
            {
                errors.Add("密码不能包含连续重复3次或以上的字符");
            }

            if (HasSequentialNumbersRegex().IsMatch(password))
            {
                errors.Add("密码不能包含连续的数字序列（如123、456）");
            }

            if (HasSequentialLettersRegex().IsMatch(password))
            {
                errors.Add("密码不能包含连续的字母序列（如abc、xyz）");
            }

            // 常见弱密码检查
            if (IsCommonWeakPassword(password))
            {
                errors.Add("密码过于简单，请使用更复杂的密码");
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// 生成密码强度评分
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>强度评分（0-100）</returns>
        public static int CalculateStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            // 基础分数 - 长度
            score += Math.Min(password.Length * 4, 40); // 最多40分

            // 复杂度加分
            if (HasUppercaseRegex().IsMatch(password))
                score += 10;

            if (HasLowercaseRegex().IsMatch(password))
                score += 10;

            if (HasDigitRegex().IsMatch(password))
                score += 10;

            if (HasSpecialCharRegex().IsMatch(password))
                score += 15;

            // 额外复杂度
            var uniqueChars = password.Distinct().Count();
            score += Math.Min(uniqueChars * 2, 15); // 最多15分

            // 扣分项
            if (HasRepeatingCharactersRegex().IsMatch(password))
                score -= 10;

            if (HasSequentialNumbersRegex().IsMatch(password))
                score -= 10;

            if (HasSequentialLettersRegex().IsMatch(password))
                score -= 10;

            if (IsCommonWeakPassword(password))
                score -= 20;

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// 获取密码强度等级
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>强度等级</returns>
        public static PasswordStrength GetStrengthLevel(string password)
        {
            var score = CalculateStrength(password);

            return score switch
            {
                >= 80 => PasswordStrength.VeryStrong,
                >= 60 => PasswordStrength.Strong,
                >= 40 => PasswordStrength.Medium,
                >= 20 => PasswordStrength.Weak,
                _ => PasswordStrength.VeryWeak
            };
        }

        /// <summary>
        /// 生成符合策略的随机密码
        /// </summary>
        /// <param name="length">密码长度（默认12）</param>
        /// <returns>随机密码</returns>
        public static string GenerateSecurePassword(int length = 12)
        {
            if (length < Policy.MinLength)
                length = Policy.MinLength;

            if (length > Policy.MaxLength)
                length = Policy.MaxLength;

            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+-=[]{}|;:,./<>?";

            var chars = new List<char>();
            var random = new Random(Guid.NewGuid().GetHashCode());

            // 确保至少包含每种类型的字符
            if (Policy.RequireUppercase)
                chars.Add(uppercase[random.Next(uppercase.Length)]);

            if (Policy.RequireLowercase)
                chars.Add(lowercase[random.Next(lowercase.Length)]);

            if (Policy.RequireDigit)
                chars.Add(digits[random.Next(digits.Length)]);

            if (Policy.RequireSpecialChar)
                chars.Add(special[random.Next(special.Length)]);

            // 填充剩余字符
            var allChars = uppercase + lowercase + digits + special;
            while (chars.Count < length)
            {
                chars.Add(allChars[random.Next(allChars.Length)]);
            }

            // 打乱顺序
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        /// <summary>
        /// 检查是否为常见弱密码
        /// </summary>
        private static bool IsCommonWeakPassword(string password)
        {
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "123456", "12345678", "qwerty", "abc123",
                "monkey", "1234567", "letmein", "trustno1", "dragon",
                "baseball", "111111", "iloveyou", "master", "sunshine",
                "ashley", "bailey", "passw0rd", "shadow", "123123",
                "654321", "superman", "qazwsx", "michael", "football",
                "password1", "password123", "admin", "administrator", "root"
            };

            return commonPasswords.Contains(password);
        }
    }

    /// <summary>
    /// 密码强度等级
    /// </summary>
    public enum PasswordStrength
    {
        /// <summary>
        /// 非常弱
        /// </summary>
        VeryWeak = 0,

        /// <summary>
        /// 弱
        /// </summary>
        Weak = 1,

        /// <summary>
        /// 中等
        /// </summary>
        Medium = 2,

        /// <summary>
        /// 强
        /// </summary>
        Strong = 3,

        /// <summary>
        /// 非常强
        /// </summary>
        VeryStrong = 4
    }
}