using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Shared.Utilities.Helpers
{

    /// <summary>
    /// 密码强度等级
    /// </summary>
    public enum PasswordStrength
    {

        /// <summary>
        /// 弱密码
        /// </summary>
        [Description("弱")]
        Weak = 1,

        /// <summary>
        /// 一般密码
        /// </summary>
        [Description("一般")]
        Fair = 2,

        /// <summary>
        /// 良好密码
        /// </summary>
        [Description("良好")]
        Good = 3,

        /// <summary>
        /// 强密码
        /// </summary>
        [Description("强")]
        Strong = 4,

        /// <summary>
        /// 很强密码
        /// </summary>
        [Description("很强")]
        VeryStrong = 5
    }

    /// <summary>
    /// 密码验证结果
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
        /// 建议信息列表
        /// </summary>
        public List<string> Suggestions { get; set; } = [];

        /// <summary>
        /// 强度评分（0-100）
        /// </summary>
        public int Score { get; set; }
    }

    /// <summary>
    /// 提供密码哈希工具，使用ASP.NET Core Identity实现
    /// </summary>
    [Description("密码工具类")]
    public static partial class PasswordHelper
    {
        private static readonly PasswordHasher<object> _hasher = new();

        // 常见弱密码列表
        private static readonly HashSet<string> WeakPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "123456789", "12345678", "12345", "1234567", "admin", "qwerty",
            "abc123", "123123", "password123", "admin123", "root", "guest", "user", "test",
            "iloveyou", "welcome", "monkey", "dragon", "letmein", "trustno1", "sunshine"
        };

        // 生成正则表达式以提升性能
        [GeneratedRegex(@"[a-z]", RegexOptions.Compiled)]
        private static partial Regex LowercaseRegex();

        [GeneratedRegex(@"[A-Z]", RegexOptions.Compiled)]
        private static partial Regex UppercaseRegex();

        [GeneratedRegex(@"[0-9]", RegexOptions.Compiled)]
        private static partial Regex DigitRegex();

        [GeneratedRegex(@"[^a-zA-Z0-9]", RegexOptions.Compiled)]
        private static partial Regex SpecialCharRegex();

        [GeneratedRegex(@"(.)\1{2,}", RegexOptions.Compiled)]
        private static partial Regex RepeatingCharRegex();

        [GeneratedRegex(@"(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex SequentialRegex();

        /// <summary>
        /// 对明文密码进行哈希
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>哈希后的密码</returns>
        public static string Hash(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        /// <summary>
        /// 验证密码与存储的哈希是否匹配
        /// </summary>
        /// <param name="hash">存储的密码哈希</param>
        /// <param name="password">待验证的明文密码</param>
        /// <returns>验证结果</returns>
        public static bool Verify(string hash, string password)
        {
            var result = _hasher.VerifyHashedPassword(null!, hash, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        /// <summary>
        /// 验证密码强度和合规性
        /// </summary>
        /// <param name="password">待验证的密码</param>
        /// <param name="minLength">最小长度（默认8）</param>
        /// <param name="maxLength">最大长度（默认128）</param>
        /// <param name="requireLowercase">是否要求小写字母</param>
        /// <param name="requireUppercase">是否要求大写字母</param>
        /// <param name="requireDigit">是否要求数字</param>
        /// <param name="requireSpecialChar">是否要求特殊字符</param>
        /// <returns>验证结果</returns>
        public static PasswordValidationResult ValidatePassword(
            string password,
            int minLength = 8,
            int maxLength = 128,
            bool requireLowercase = true,
            bool requireUppercase = true,
            bool requireDigit = true,
            bool requireSpecialChar = true)
        {
            var result = new PasswordValidationResult();

            if (string.IsNullOrEmpty(password))
            {
                result.Errors.Add("密码不能为空");
                return result;
            }

            // 长度检查
            if (password.Length < minLength)
            {
                result.Errors.Add($"密码长度不能少于{minLength}位");
            }

            if (password.Length > maxLength)
            {
                result.Errors.Add($"密码长度不能超过{maxLength}位");
            }

            // 字符类型检查
            bool hasLower = LowercaseRegex().IsMatch(password);
            bool hasUpper = UppercaseRegex().IsMatch(password);
            bool hasDigit = DigitRegex().IsMatch(password);
            bool hasSpecial = SpecialCharRegex().IsMatch(password);

            if (requireLowercase && !hasLower)
            {
                result.Errors.Add("密码必须包含小写字母");
            }

            if (requireUppercase && !hasUpper)
            {
                result.Errors.Add("密码必须包含大写字母");
            }

            if (requireDigit && !hasDigit)
            {
                result.Errors.Add("密码必须包含数字");
            }

            if (requireSpecialChar && !hasSpecial)
            {
                result.Errors.Add("密码必须包含特殊字符");
            }

            // 弱密码检查
            if (WeakPasswords.Contains(password))
            {
                result.Errors.Add("密码过于简单，请使用更复杂的密码");
            }

            // 重复字符检查
            if (RepeatingCharRegex().IsMatch(password))
            {
                result.Errors.Add("密码不能包含连续重复的字符");
            }

            // 连续字符检查
            if (SequentialRegex().IsMatch(password))
            {
                result.Errors.Add("密码不能包含连续的字符序列");
            }

            // 计算强度得分
            var score = CalculatePasswordScore(password, hasLower, hasUpper, hasDigit, hasSpecial);
            result.Score = score;
            result.Strength = GetPasswordStrength(score);

            // 生成建议
            if (score < 60)
            {
                if (!hasLower)
                {
                    result.Suggestions.Add("添加小写字母");
                }

                if (!hasUpper)
                {
                    result.Suggestions.Add("添加大写字母");
                }

                if (!hasDigit)
                {
                    result.Suggestions.Add("添加数字");
                }

                if (!hasSpecial)
                {
                    result.Suggestions.Add("添加特殊字符");
                }

                if (password.Length < 12)
                {
                    result.Suggestions.Add("增加密码长度到12位以上");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 计算密码强度得分
        /// </summary>
        private static int CalculatePasswordScore(string password, bool hasLower, bool hasUpper, bool hasDigit, bool hasSpecial)
        {
            var score = 0;

            // 长度得分 (0-25分)
            score += Math.Min(password.Length * 2, 25);

            // 字符类型得分 (每种类型15分，最多60分)
            if (hasLower)
            {
                score += 15;
            }

            if (hasUpper)
            {
                score += 15;
            }

            if (hasDigit)
            {
                score += 15;
            }

            if (hasSpecial)
            {
                score += 15;
            }

            // 唯一字符数量得分 (0-15分)
            var uniqueChars = password.Distinct().Count();
            score += Math.Min(uniqueChars * 2, 15);

            // 惩罚项
            if (WeakPasswords.Contains(password))
            {
                score -= 30;
            }

            if (RepeatingCharRegex().IsMatch(password))
            {
                score -= 15;
            }

            if (SequentialRegex().IsMatch(password))
            {
                score -= 15;
            }

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// 根据得分获取密码强度等级
        /// </summary>
        private static PasswordStrength GetPasswordStrength(int score)
        {
            return score switch
            {
                >= 80 => PasswordStrength.VeryStrong,
                >= 60 => PasswordStrength.Strong,
                >= 40 => PasswordStrength.Good,
                >= 20 => PasswordStrength.Fair,
                _ => PasswordStrength.Weak
            };
        }

        /// <summary>
        /// 生成安全的随机密码
        /// </summary>
        /// <param name="length">密码长度（默认12）</param>
        /// <param name="includeLowercase">包含小写字母</param>
        /// <param name="includeUppercase">包含大写字母</param>
        /// <param name="includeDigits">包含数字</param>
        /// <param name="includeSpecialChars">包含特殊字符</param>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword(
            int length = 12,
            bool includeLowercase = true,
            bool includeUppercase = true,
            bool includeDigits = true,
            bool includeSpecialChars = true)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var chars = string.Empty;
            if (includeLowercase)
            {
                chars += lowercase;
            }

            if (includeUppercase)
            {
                chars += uppercase;
            }

            if (includeDigits)
            {
                chars += digits;
            }

            if (includeSpecialChars)
            {
                chars += specialChars;
            }

            if (string.IsNullOrEmpty(chars))
            {
                throw new ArgumentException("至少要包含一种字符类型");
            }

            using var rng = RandomNumberGenerator.Create();
            var password = new StringBuilder();

            // 确保每种要求的字符类型至少出现一次
            if (includeLowercase)
            {
                password.Append(GetRandomChar(lowercase, rng));
            }

            if (includeUppercase)
            {
                password.Append(GetRandomChar(uppercase, rng));
            }

            if (includeDigits)
            {
                password.Append(GetRandomChar(digits, rng));
            }

            if (includeSpecialChars)
            {
                password.Append(GetRandomChar(specialChars, rng));
            }

            // 填充剩余长度
            for (int i = password.Length; i < length; i++)
            {
                password.Append(GetRandomChar(chars, rng));
            }

            // 随机打乱字符顺序
            return ShuffleString(password.ToString(), rng);
        }

        /// <summary>
        /// 获取随机字符
        /// </summary>
        private static char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            return chars[(int)(randomValue % (uint)chars.Length)];
        }

        /// <summary>
        /// 随机打乱字符串
        /// </summary>
        private static string ShuffleString(string input, RandomNumberGenerator rng)
        {
            var array = input.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                var randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                var j = (int)(BitConverter.ToUInt32(randomBytes, 0) % (uint)(i + 1));
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new string(array);
        }

        /// <summary>
        /// 检查密码是否需要重新哈希（用于密码策略升级）
        /// </summary>
        /// <param name="hash">密码哈希</param>
        /// <param name="password">明文密码</param>
        /// <returns>是否需要重新哈希</returns>
        public static bool NeedsRehash(string hash, string password)
        {
            var result = _hasher.VerifyHashedPassword(null!, hash, password);
            return result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        /// <summary>
        /// 生成用于密码重置的临时密码
        /// </summary>
        /// <returns>临时密码</returns>
        public static string GenerateTemporaryPassword()
        {
            return GenerateSecurePassword(8, includeSpecialChars: false);
        }

        /// <summary>
        /// 检查两个密码是否相同（避免时序攻击）
        /// </summary>
        /// <param name="password1">密码1</param>
        /// <param name="password2">密码2</param>
        /// <returns>是否相同</returns>
        public static bool SecureEquals(string password1, string password2)
        {
            if (password1.Length != password2.Length)
            {
                return false;
            }

            var result = 0;
            for (int i = 0; i < password1.Length; i++)
            {
                result |= password1[i] ^ password2[i];
            }

            return result == 0;
        }
    }
}
