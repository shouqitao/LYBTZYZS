using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Shared.Utilities.Security
{
    // OpenSpec: unify-enums-to-shared - PasswordStrength已迁移到LYBT.Shared.Models.Enums.SecurityEnums.cs

    /// <summary>
    /// 统一密码帮助类 - 集成密码哈希、验证、强度检查和生成功能
    /// 解决密码操作分散在多个文件中的问题，提供统一的密码处理接口
    /// 使用BCrypt算法确保密码安全性，整合了PasswordLegacyHelper的密码验证功能
    /// </summary>
    public static class PasswordHelper
    {
        #region 配置常量

        /// <summary>
        /// 默认BCrypt工作因子
        /// </summary>
        private const int DefaultWorkFactor = 11;

        /// <summary>
        /// 最小工作因子
        /// </summary>
        private const int MinWorkFactor = 10;

        /// <summary>
        /// 最大工作因子
        /// </summary>
        private const int MaxWorkFactor = 15;

        /// <summary>
        /// 随机字节长度
        /// </summary>
        private const int RandomByteLength = 32;

        /// <summary>
        /// 当前工作因子
        /// </summary>
        public static int WorkFactor { get; private set; } = DefaultWorkFactor;

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

        #region 核心密码操作

        /// <summary>
        /// 哈希密码（统一BCrypt接口）
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="userType">用户类型</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>哈希后的密码</returns>
        public static string HashPassword(string password, UserRole userType = UserRole.Doctor, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                logger?.LogError("密码哈希失败: 密码为空 [用户类型: {UserType}] [时间: {Timestamp}]",
                    userType, DateTime.UtcNow);
                throw new ArgumentException("密码不能为空", nameof(password));
            }

            try
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

                logger?.LogInformation("密码哈希成功 [用户类型: {UserType}] [工作因子: {WorkFactor}] [时间: {Timestamp}]",
                    userType, WorkFactor, DateTime.UtcNow);

                return hashedPassword;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "密码哈希失败 [用户类型: {UserType}] [时间: {Timestamp}]",
                    userType, DateTime.UtcNow);
                throw;
            }
        }

        /// <summary>
        /// 验证密码（统一BCrypt接口）
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="hashedPassword">哈希密码</param>
        /// <param name="userType">用户类型</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>验证结果</returns>
        public static PasswordVerificationResult VerifyPassword(string password, string hashedPassword,
            UserRole userType = UserRole.Doctor, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            {
                return new PasswordVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "密码或哈希值为空",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                // 验证密码
                bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

                // 检查是否需要重新哈希（工作因子不匹配）
                bool needsRehash = isValid && BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, WorkFactor);
                string? newHashedPassword = null;

                if (needsRehash)
                {
                    newHashedPassword = HashPassword(password, userType, logger);
                    logger?.LogWarning("密码重新哈希 [用户类型: {UserType}] [原因: 工作因子升级] [时间: {Timestamp}]",
                        userType, DateTime.UtcNow);
                }

                logger?.LogInformation("密码验证结果 [用户类型: {UserType}] [成功: {Success}] [需要重新哈希: {NeedsRehash}] [时间: {Timestamp}]",
                    userType, isValid, needsRehash, DateTime.UtcNow);

                return new PasswordVerificationResult
                {
                    IsSuccess = isValid,
                    NeedsRehash = needsRehash,
                    NewHashedPassword = newHashedPassword,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "密码验证失败 [用户类型: {UserType}] [时间: {Timestamp}]",
                    userType, DateTime.UtcNow);

                return new PasswordVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "密码验证过程中发生错误",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// 验证并重新哈希密码（如果需要）
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="hashedPassword">哈希密码</param>
        /// <param name="userType">用户类型</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>验证结果</returns>
        public static PasswordVerificationResult VerifyAndRehashIfNeeded(string password, string hashedPassword,
            UserRole userType = UserRole.Doctor, ILogger? logger = null)
        {
            var result = VerifyPassword(password, hashedPassword, userType, logger);

            if (result.IsSuccess && result.NeedsRehash)
            {
                result.NewHashedPassword = HashPassword(password, userType, logger);
            }

            return result;
        }

        /// <summary>
        /// 生成临时密码 (Issue #1162, #1760)
        /// 格式：大写字母(1) + 小写字母(4) + 数字(3) = 8位
        /// 示例：Abcd123
        /// </summary>
        /// <returns>8位临时密码</returns>
        public static string GenerateTemporaryPassword()
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";

            var random = new Random();
            var password = new char[8];

            // 1个大写字母
            password[0] = upperChars[random.Next(upperChars.Length)];

            // 4个小写字母
            for (int i = 1; i <= 4; i++)
            {
                password[i] = lowerChars[random.Next(lowerChars.Length)];
            }

            // 3个数字
            for (int i = 5; i < 8; i++)
            {
                password[i] = numberChars[random.Next(numberChars.Length)];
            }

            return new string(password);
        }

        /// <summary>
        /// 生成安全的随机盐值
        /// </summary>
        /// <param name="length">盐值长度</param>
        /// <returns>盐值</returns>
        public static string GenerateSalt(int length = RandomByteLength)
        {
            var randomBytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        #endregion

        #region 配置管理

        /// <summary>
        /// 更新工作因子
        /// </summary>
        /// <param name="newWorkFactor">新的工作因子</param>
        /// <returns>是否更新成功</returns>
        public static bool UpdateWorkFactor(int newWorkFactor)
        {
            if (newWorkFactor >= MinWorkFactor && newWorkFactor <= MaxWorkFactor)
            {
                WorkFactor = newWorkFactor;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取当前配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        public static PasswordHelperConfiguration GetConfiguration()
        {
            return new PasswordHelperConfiguration
            {
                WorkFactor = WorkFactor,
                EnableRehashing = true,
                PasswordHistoryCount = 5,
                DefaultWorkFactor = DefaultWorkFactor,
                MinWorkFactor = MinWorkFactor,
                MaxWorkFactor = MaxWorkFactor
            };
        }

        #endregion

        #region 密码强度验证功能

        /// <summary>
        /// 验证密码强度和合规性（从PasswordLegacyHelper迁移）
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
            bool hasSpecial = !Regex.IsMatch(password, "[a-zA-Z0-9]");

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

        /// <summary>
        /// 安全字符串比较，防止时间攻击（从PasswordLegacyHelper迁移）
        /// </summary>
        /// <param name="password1">密码1</param>
        /// <param name="password2">密码2</param>
        /// <returns>是否相同</returns>
        public static bool SecureEquals(string? password1, string? password2)
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

        #endregion

        #region 支持类型

        /// <summary>
        /// 密码验证结果（BCrypt验证）
        /// </summary>
        public class PasswordVerificationResult
        {
            /// <summary>
            /// 是否验证成功
            /// </summary>
            public bool IsSuccess { get; set; }

            /// <summary>
            /// 是否需要重新哈希
            /// </summary>
            public bool NeedsRehash { get; set; }

            /// <summary>
            /// 新的哈希密码（如果需要重新哈希）
            /// </summary>
            public string? NewHashedPassword { get; set; }

            /// <summary>
            /// 错误消息
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// 验证时间戳
            /// </summary>
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// 密码帮助类配置信息
        /// </summary>
        public class PasswordHelperConfiguration
        {
            /// <summary>
            /// 当前工作因子
            /// </summary>
            public int WorkFactor { get; set; }

            /// <summary>
            /// 是否启用重新哈希
            /// </summary>
            public bool EnableRehashing { get; set; }

            /// <summary>
            /// 密码历史记录数量
            /// </summary>
            public int PasswordHistoryCount { get; set; }

            /// <summary>
            /// 默认工作因子
            /// </summary>
            public int DefaultWorkFactor { get; set; }

            /// <summary>
            /// 最小工作因子
            /// </summary>
            public int MinWorkFactor { get; set; }

            /// <summary>
            /// 最大工作因子
            /// </summary>
            public int MaxWorkFactor { get; set; }
        }

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