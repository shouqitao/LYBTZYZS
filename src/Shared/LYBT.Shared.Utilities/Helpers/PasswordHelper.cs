using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
        /// 建议信息
        /// </summary>
        public string Suggestions { get; set; } = string.Empty;
    }

    /// <summary>
    /// 密码安全工具类，专为小型中医诊所系统设计
    /// 提供密码哈希、验证、生成和基础强度检查功能
    /// </summary>
    [Description("密码工具类")]
    public static partial class PasswordHelper
    {
        private const int SaltSize = 32; // 256 bits
        private const int KeySize = 64;  // 512 bits
        private const int Iterations = 100_000;

        // 常见弱密码列表（23个）
        private static readonly HashSet<string> WeakPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "admin", "123456789", "qwerty",
            "abc123", "password123", "admin123", "123123", "111111",
            "666666", "888888", "1234567890", "root", "user",
            "guest", "test", "welcome", "letmein", "monkey",
            "dragon", "master", "123abc"
        };

        // 生成正则表达式以提升性能
        [GeneratedRegex(@"[a-zA-Z0-9]")]
        private static partial Regex AlphanumericRegex();

        /// <summary>
        /// 对明文密码进行哈希
        /// 使用 PBKDF2 算法
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>哈希后的密码</returns>
        public static string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);

            // 生成随机盐
            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            // 使用PBKDF2进行密码散列
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            // 将盐和哈希值组合并编码为Base64
            var result = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, result, 0, SaltSize);
            Array.Copy(hash, 0, result, SaltSize, KeySize);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// 验证密码与存储的哈希是否匹配
        /// </summary>
        /// <param name="hash">存储的密码哈希</param>
        /// <param name="password">待验证的明文密码</param>
        /// <returns>验证结果</returns>
        public static bool Verify(string hash, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(hash);
            ArgumentException.ThrowIfNullOrEmpty(password);

            try
            {
                // 解码存储的哈希
                var hashBytes = Convert.FromBase64String(hash);

                // 提取盐
                var salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // 提取存储的哈希值
                var storedHash = new byte[KeySize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, KeySize);

                // 使用相同的盐对输入密码进行哈希
                var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    Iterations,
                    HashAlgorithmName.SHA256,
                    KeySize);

                // 安全比较哈希值
                return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全字符串比较，防止时间攻击
        /// </summary>
        /// <param name="password1">密码1</param>
        /// <param name="password2">密码2</param>
        /// <returns>是否相同</returns>
        public static bool SecureEquals(string password1, string password2)
        {
            if (password1 == null || password2 == null)
                return password1 == password2;
                
            if (password1.Length != password2.Length)
                return false;

            var result = 0;
            for (int i = 0; i < password1.Length; i++)
            {
                result |= password1[i] ^ password2[i];
            }

            return result == 0;
        }

        /// <summary>
        /// 验证密码强度和合规性
        /// </summary>
        /// <param name="password">待验证的密码</param>
        /// <param name="minLength">最小长度（默认8）</param>
        /// <param name="requireUppercase">是否要求大写字母</param>
        /// <param name="requireLowercase">是否要求小写字母</param>
        /// <param name="requireDigits">是否要求数字</param>
        /// <param name="requireSpecialChars">是否要求特殊字符</param>
        /// <returns>验证结果</returns>
        public static PasswordValidationResult ValidatePassword(
            string password,
            int minLength = 8,
            bool requireUppercase = true,
            bool requireLowercase = true,
            bool requireDigits = true,
            bool requireSpecialChars = true)
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

            // 字符类型检查
            bool hasLower = password.Any(char.IsLower);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = !AlphanumericRegex().IsMatch(password);

            if (requireLowercase && !hasLower)
                result.Errors.Add("密码必须包含小写字母");
            if (requireUppercase && !hasUpper)
                result.Errors.Add("密码必须包含大写字母");
            if (requireDigits && !hasDigit)
                result.Errors.Add("密码必须包含数字");
            if (requireSpecialChars && !hasSpecial)
                result.Errors.Add("密码必须包含特殊字符");

            // 弱密码检查
            if (IsCommonPassword(password))
            {
                result.Errors.Add("密码过于简单，请使用更复杂的密码");
            }

            // 计算密码强度
            result.Strength = CheckPasswordStrength(password);
            result.IsValid = result.Errors.Count == 0;

            // 生成建议
            if (result.Errors.Count > 0)
            {
                result.Suggestions = "建议：" + string.Join("，", result.Errors.Take(3));
            }

            return result;
        }

        /// <summary>
        /// 检查密码强度
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
            if (!AlphanumericRegex().IsMatch(password)) score += 10;

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
        /// 检查是否为常见弱密码
        /// </summary>
        /// <param name="password">密码</param>
        /// <returns>是否为弱密码</returns>
        public static bool IsCommonPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && WeakPasswords.Contains(password);
        }

        /// <summary>
        /// 检查密码长度是否符合最小要求
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="minLength">最小长度</param>
        /// <returns>是否符合要求</returns>
        public static bool HasMinimumLength(string password, int minLength)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= minLength;
        }

        /// <summary>
        /// 生成安全的随机密码
        /// </summary>
        /// <param name="length">密码长度（默认12）</param>
        /// <param name="includeUppercase">包含大写字母</param>
        /// <param name="includeLowercase">包含小写字母</param>
        /// <param name="includeDigits">包含数字</param>
        /// <param name="includeSpecialChars">包含特殊字符</param>
        /// <returns>生成的随机密码</returns>
        public static string GenerateSecurePassword(
            int length = 12,
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
                password.Append(lowercase[Random.Shared.Next(lowercase.Length)]);
            }
            if (includeUppercase)
            {
                chars.Append(uppercase);
                password.Append(uppercase[Random.Shared.Next(uppercase.Length)]);
            }
            if (includeDigits)
            {
                chars.Append(digits);
                password.Append(digits[Random.Shared.Next(digits.Length)]);
            }
            if (includeSpecialChars)
            {
                chars.Append(specialChars);
                password.Append(specialChars[Random.Shared.Next(specialChars.Length)]);
            }

            if (chars.Length == 0)
                throw new ArgumentException("至少要包含一种字符类型");

            // 填充剩余长度
            var allChars = chars.ToString();
            while (password.Length < length)
            {
                password.Append(allChars[Random.Shared.Next(allChars.Length)]);
            }

            // 简单打乱字符顺序
            var result = password.ToString().ToCharArray();
            for (int i = result.Length - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return new string(result);
        }
    }
}