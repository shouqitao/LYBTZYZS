using LYBT.Infrastructure.Configuration.Options;
using AuthOptions = LYBT.Infrastructure.Configuration.Options.AuthOptions;
using SecurityOptions = LYBT.Infrastructure.Configuration.Options.SecurityOptions;
using LYBT.Module.Users;
using Microsoft.Extensions.Options;
using System.Net;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 安全配置验证器接口
    /// </summary>
    public interface ISecurityConfigurationValidator
    {
        /// <summary>
        /// 验证安全配置
        /// </summary>
        Task<SecurityValidationResult> ValidateConfigurationAsync();

        /// <summary>
        /// 验证JWT配置
        /// </summary>
        Task<SecurityValidationResult> ValidateJwtConfigurationAsync();

        /// <summary>
        /// 验证环境安全配置
        /// </summary>
        Task<SecurityValidationResult> ValidateEnvironmentSecurityAsync();
    }

    /// <summary>
    /// 安全配置验证器实现
    /// </summary>
    public class SecurityConfigurationValidator : ISecurityConfigurationValidator
    {
        private readonly SecurityOptions _securityOptions;
        private readonly JwtOptions _jwtOptions;
        private readonly SysAdminOptions _sysAdminOptions;
        private readonly UserOptions _userOptions;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityConfigurationValidator> _logger;

        public SecurityConfigurationValidator(
            IOptions<SecurityOptions> securityOptions,
            IOptions<JwtOptions> jwtOptions,
            IOptions<SysAdminOptions> sysAdminOptions,
            IOptions<UserOptions> userOptions,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<SecurityConfigurationValidator> logger)
        {
            _securityOptions = securityOptions.Value;
            _jwtOptions = jwtOptions.Value;
            _sysAdminOptions = sysAdminOptions.Value;
            _userOptions = userOptions.Value;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SecurityValidationResult> ValidateConfigurationAsync()
        {
            var result = new SecurityValidationResult();

            // 验证JWT配置
            var jwtValidation = await ValidateJwtConfigurationAsync();
            result.MergeWith(jwtValidation);

            // 验证环境安全配置
            var envValidation = await ValidateEnvironmentSecurityAsync();
            result.MergeWith(envValidation);

            // 验证CORS配置
            ValidateCorsConfiguration(result);

            // 验证密码策略
            ValidatePasswordPolicy(result);

            // 验证HTTPS配置
            ValidateHttpsConfiguration(result);

            return result;
        }

        public async Task<SecurityValidationResult> ValidateJwtConfigurationAsync()
        {
            var result = new SecurityValidationResult();

            // 检查JWT密钥强度
            if (string.IsNullOrEmpty(_jwtOptions.Secret))
            {
                result.AddError("JWT密钥不能为空", SecurityValidationLevel.Critical);
            }
            else if (_jwtOptions.Secret.Length < 32)
            {
                result.AddError("JWT密钥长度至少需要32个字符", SecurityValidationLevel.High);
            }
            else if (IsWeakJwtSecret(_jwtOptions.Secret))
            {
                result.AddError("JWT密钥强度不足，建议使用更复杂的密钥", SecurityValidationLevel.Medium);
            }

            // 检查JWT过期时间
            if (_jwtOptions.ExpireMinutes > 1440) // 超过24小时
            {
                result.AddWarning("JWT过期时间过长，建议设置为更短的时间以提高安全性");
            }

            // 检查RememberMe过期时间
            if (_jwtOptions.RememberMeExpireMinutes > 43200) // 超过30天
            {
                result.AddWarning("RememberMe过期时间过长，存在安全风险");
            }

            await Task.CompletedTask;
            return result;
        }

        public async Task<SecurityValidationResult> ValidateEnvironmentSecurityAsync()
        {
            var result = new SecurityValidationResult();

            // 生产环境安全检查
            if (_environment.IsProduction())
            {
                // 检查是否启用了敏感数据日志记录
                if (_securityOptions.Environment.EnableSensitiveDataLogging)
                {
                    result.AddError("生产环境不应启用敏感数据日志记录", SecurityValidationLevel.High);
                }

                // 检查是否隐藏了详细错误信息
                if (!_securityOptions.Environment.HideDetailedErrors)
                {
                    result.AddWarning("生产环境应隐藏详细错误信息");
                }

                // 检查AllowedHosts配置
                var allowedHosts = _configuration["AllowedHosts"];
                if (string.IsNullOrEmpty(allowedHosts) || allowedHosts == "*")
                {
                    result.AddError("生产环境必须配置具体的AllowedHosts", SecurityValidationLevel.High);
                }
            }

            // 检查默认密码配置
            ValidateDefaultPasswords(result);

            await Task.CompletedTask;
            return result;
        }

        private void ValidateCorsConfiguration(SecurityValidationResult result)
        {
            var corsConfig = _securityOptions.Cors;

            // 检查是否允许所有源
            if (corsConfig.AllowedOrigins.Contains("*"))
            {
                if (_environment.IsProduction())
                {
                    result.AddError("生产环境不应允许所有CORS源", SecurityValidationLevel.High);
                }
                else
                {
                    result.AddWarning("开发环境允许所有CORS源可能存在安全风险");
                }
            }

            // 检查是否配置了具体的源
            if (!corsConfig.AllowedOrigins.Any() && _environment.IsProduction())
            {
                result.AddWarning("生产环境应配置具体的CORS源");
            }
        }

        private void ValidatePasswordPolicy(SecurityValidationResult result)
        {
            var policy = _securityOptions.PasswordPolicy;

            if (policy.MinLength < 8)
            {
                result.AddError("密码最小长度不应少于8个字符", SecurityValidationLevel.Medium);
            }
            else if (policy.MinLength < 12)
            {
                result.AddWarning("建议密码最小长度设置为12个字符或以上");
            }

            if (!policy.RequireUppercase || !policy.RequireLowercase || 
                !policy.RequireDigit || !policy.RequireSpecialChar)
            {
                result.AddWarning("建议启用所有密码复杂性要求");
            }
        }

        private void ValidateHttpsConfiguration(SecurityValidationResult result)
        {
            var httpsConfig = _securityOptions.Https;

            if (!httpsConfig.RequireHttps && _environment.IsProduction())
            {
                result.AddError("生产环境必须强制使用HTTPS", SecurityValidationLevel.Critical);
            }

            if (httpsConfig.HstsMaxAgeDays < 30)
            {
                result.AddWarning("HSTS最大年龄建议至少设置为30天");
            }
        }

        private void ValidateDefaultPasswords(SecurityValidationResult result)
        {
            // 从配置选项获取密码（优先使用环境变量）
            var sysAdminPassword = _sysAdminOptions?.DefaultPassword;
            var userPassword = _userOptions?.DefaultUserPassword;

            if (!string.IsNullOrEmpty(sysAdminPassword) && IsWeakPassword(sysAdminPassword))
            {
                result.AddError("系统管理员默认密码过于简单", SecurityValidationLevel.High);
            }

            if (!string.IsNullOrEmpty(userPassword) && IsWeakPassword(userPassword))
            {
                result.AddWarning("用户默认密码过于简单，建议设置更复杂的密码");
            }
        }

        private static bool IsWeakJwtSecret(string secret)
        {
            // 检查是否包含明显的弱模式
            var weakPatterns = new[]
            {
                "secret",
                "password",
                "123456",
                "qwerty",
                "admin",
                "test"
            };

            return weakPatterns.Any(pattern => 
                secret.ToLower().Contains(pattern.ToLower()));
        }

        private static bool IsWeakPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return true;

            var weakPasswords = new[]
            {
                "password",
                "123456",
                "qwerty",
                "admin",
                "user",
                "test",
                "changeme"
            };

            return weakPasswords.Any(weak => 
                password.ToLower().Contains(weak.ToLower()));
        }
    }

    /// <summary>
    /// 安全验证结果
    /// </summary>
    public class SecurityValidationResult
    {
        private readonly List<SecurityValidationIssue> _issues = new();

        /// <summary>
        /// 是否通过验证
        /// </summary>
        public bool IsValid => !_issues.Any(i => i.Level == SecurityValidationLevel.Critical);

        /// <summary>
        /// 是否有警告
        /// </summary>
        public bool HasWarnings => _issues.Any(i => i.Level == SecurityValidationLevel.Medium || 
                                                   i.Level == SecurityValidationLevel.Low);

        /// <summary>
        /// 问题列表
        /// </summary>
        public IReadOnlyList<SecurityValidationIssue> Issues => _issues.AsReadOnly();

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string message, SecurityValidationLevel level = SecurityValidationLevel.High)
        {
            _issues.Add(new SecurityValidationIssue(message, level, SecurityValidationIssueType.Error));
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string message, SecurityValidationLevel level = SecurityValidationLevel.Medium)
        {
            _issues.Add(new SecurityValidationIssue(message, level, SecurityValidationIssueType.Warning));
        }

        /// <summary>
        /// 添加信息
        /// </summary>
        public void AddInfo(string message)
        {
            _issues.Add(new SecurityValidationIssue(message, SecurityValidationLevel.Low, SecurityValidationIssueType.Info));
        }

        /// <summary>
        /// 合并其他验证结果
        /// </summary>
        public void MergeWith(SecurityValidationResult other)
        {
            _issues.AddRange(other._issues);
        }
    }

    /// <summary>
    /// 安全验证问题
    /// </summary>
    public record SecurityValidationIssue(
        string Message,
        SecurityValidationLevel Level,
        SecurityValidationIssueType Type);

    /// <summary>
    /// 安全验证级别
    /// </summary>
    public enum SecurityValidationLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// 安全验证问题类型
    /// </summary>
    public enum SecurityValidationIssueType
    {
        Info,
        Warning,
        Error
    }
}