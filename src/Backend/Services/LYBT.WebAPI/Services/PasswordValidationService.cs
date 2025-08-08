using LYBT.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 密码验证服务接口
    /// </summary>
    public interface IPasswordValidationService
    {
        /// <summary>
        /// 验证密码强度
        /// </summary>
        Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? username = null);

        /// <summary>
        /// 生成安全密码
        /// </summary>
        string GenerateSecurePassword(int length = 16);

        /// <summary>
        /// 检查密码是否过期
        /// </summary>
        bool IsPasswordExpired(DateTime passwordCreatedDate);
    }

    /// <summary>
    /// 密码验证服务实现
    /// </summary>
    public class PasswordValidationService : IPasswordValidationService
    {
        private readonly SecurityOptions _securityOptions;
        private readonly ILogger<PasswordValidationService> _logger;

        public PasswordValidationService(
            IOptions<SecurityOptions> securityOptions,
            ILogger<PasswordValidationService> logger)
        {
            _securityOptions = securityOptions.Value;
            _logger = logger;
        }

        public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? username = null)
        {
            var result = new PasswordValidationResult();

            if (string.IsNullOrEmpty(password))
            {
                result.AddError("密码不能为空");
                return result;
            }

            var policy = _securityOptions.PasswordPolicy;

            // 检查长度
            if (password.Length < policy.MinLength)
            {
                result.AddError($"密码长度至少需要 {policy.MinLength} 个字符");
            }

            // 检查大写字母
            if (policy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                result.AddError("密码必须包含至少一个大写字母");
            }

            // 检查小写字母
            if (policy.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                result.AddError("密码必须包含至少一个小写字母");
            }

            // 检查数字
            if (policy.RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
            {
                result.AddError("密码必须包含至少一个数字");
            }

            // 检查特殊字符
            if (policy.RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                result.AddError("密码必须包含至少一个特殊字符");
            }

            // 检查禁止的模式
            foreach (var pattern in policy.ForbiddenPatterns)
            {
                if (password.ToLower().Contains(pattern.ToLower()))
                {
                    result.AddError($"密码不能包含常见的不安全模式：{pattern}");
                }
            }

            // 检查与用户名的相似性
            if (!string.IsNullOrEmpty(username) && 
                password.ToLower().Contains(username.ToLower()))
            {
                result.AddError("密码不能包含用户名");
            }

            // 检查重复字符
            if (HasTooManyRepeatingCharacters(password))
            {
                result.AddError("密码不能包含过多重复字符");
            }

            // 检查键盘模式
            if (HasKeyboardPattern(password))
            {
                result.AddError("密码不能包含键盘连续字符模式");
            }

            await Task.CompletedTask;
            return result;
        }

        public string GenerateSecurePassword(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            var password = new List<char>();

            // 确保至少包含一个每种类型的字符
            password.Add(upperCase[random.Next(upperCase.Length)]);
            password.Add(lowerCase[random.Next(lowerCase.Length)]);
            password.Add(digits[random.Next(digits.Length)]);
            password.Add(specials[random.Next(specials.Length)]);

            // 填充剩余字符
            for (int i = password.Count; i < length; i++)
            {
                password.Add(chars[random.Next(chars.Length)]);
            }

            // 打乱密码字符顺序
            for (int i = 0; i < password.Count; i++)
            {
                int j = random.Next(i, password.Count);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password.ToArray());
        }

        public bool IsPasswordExpired(DateTime passwordCreatedDate)
        {
            var policy = _securityOptions.PasswordPolicy;
            
            if (policy.PasswordExpiryDays <= 0)
            {
                return false; // 密码不过期
            }

            var expiryDate = passwordCreatedDate.AddDays(policy.PasswordExpiryDays);
            return DateTime.UtcNow > expiryDate;
        }

        private static bool HasTooManyRepeatingCharacters(string password)
        {
            const int maxRepeats = 3;
            
            for (int i = 0; i <= password.Length - maxRepeats; i++)
            {
                var currentChar = password[i];
                var count = 1;
                
                for (int j = i + 1; j < password.Length && j < i + maxRepeats; j++)
                {
                    if (password[j] == currentChar)
                        count++;
                    else
                        break;
                }
                
                if (count >= maxRepeats)
                    return true;
            }
            
            return false;
        }

        private static bool HasKeyboardPattern(string password)
        {
            var keyboardRows = new[]
            {
                "qwertyuiop",
                "asdfghjkl",
                "zxcvbnm",
                "1234567890"
            };

            var lowerPassword = password.ToLower();
            
            foreach (var row in keyboardRows)
            {
                for (int i = 0; i <= row.Length - 3; i++)
                {
                    var pattern = row.Substring(i, 3);
                    if (lowerPassword.Contains(pattern))
                        return true;
                    
                    // 检查反向模式
                    var reversePattern = new string(pattern.Reverse().ToArray());
                    if (lowerPassword.Contains(reversePattern))
                        return true;
                }
            }
            
            return false;
        }
    }

    /// <summary>
    /// 密码验证结果
    /// </summary>
    public class PasswordValidationResult
    {
        private readonly List<string> _errors = new();

        /// <summary>
        /// 是否验证成功
        /// </summary>
        public bool IsValid => !_errors.Any();

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// 获取错误信息字符串
        /// </summary>
        public string GetErrorMessage()
        {
            return string.Join("; ", _errors);
        }
    }
}