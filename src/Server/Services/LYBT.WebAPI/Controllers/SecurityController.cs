using Asp.Versioning;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 安全管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SecurityController : BaseController
    {
        private readonly ISecurityConfigurationValidator _securityValidator;
        private readonly IPasswordValidationService _passwordValidator;

        public SecurityController(
            ISecurityConfigurationValidator securityValidator,
            IPasswordValidationService passwordValidator,
            ILogger<SecurityController> logger)
            : base(logger)
        {
            _securityValidator = securityValidator;
            _passwordValidator = passwordValidator;
        }

        /// <summary>
        /// 获取安全配置验证结果
        /// </summary>
        [HttpGet("configuration/validation")]
        public async Task<ActionResult<SecurityConfigurationValidationDto>> ValidateSecurityConfiguration()
        {
            try
            {
                var result = await _securityValidator.ValidateConfigurationAsync();
                
                var dto = new SecurityConfigurationValidationDto
                {
                    IsValid = result.IsValid,
                    HasWarnings = result.HasWarnings,
                    Issues = result.Issues.Select(i => new SecurityIssueDto
                    {
                        Message = i.Message,
                        Level = i.Level.ToString(),
                        Type = i.Type.ToString()
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证安全配置");
            }
        }

        /// <summary>
        /// 验证JWT配置
        /// </summary>
        [HttpGet("jwt/validation")]
        public async Task<ActionResult<SecurityConfigurationValidationDto>> ValidateJwtConfiguration()
        {
            try
            {
                var result = await _securityValidator.ValidateJwtConfigurationAsync();
                
                var dto = new SecurityConfigurationValidationDto
                {
                    IsValid = result.IsValid,
                    HasWarnings = result.HasWarnings,
                    Issues = result.Issues.Select(i => new SecurityIssueDto
                    {
                        Message = i.Message,
                        Level = i.Level.ToString(),
                        Type = i.Type.ToString()
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证JWT配置");
            }
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        [HttpPost("password/validation")]
        public async Task<ActionResult<PasswordValidationDto>> ValidatePassword([FromBody] ValidatePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("密码不能为空");
                }

                var result = await _passwordValidator.ValidatePasswordAsync(request.Password, request.Username);
                
                var dto = new PasswordValidationDto
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors.ToList(),
                    ErrorMessage = result.GetErrorMessage()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "验证密码强度");
            }
        }

        /// <summary>
        /// 生成安全密码
        /// </summary>
        [HttpPost("password/generate")]
        public ActionResult<GeneratedPasswordDto> GenerateSecurePassword([FromBody] GeneratePasswordRequest request)
        {
            try
            {
                var length = request.Length is >= 8 and <= 128 ? request.Length : 16;
                var password = _passwordValidator.GenerateSecurePassword(length);
                
                var dto = new GeneratedPasswordDto
                {
                    Password = password,
                    Length = password.Length,
                    GeneratedAt = DateTime.UtcNow
                };

                LogOperation("生成安全密码", new { Length = length }, null);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "生成安全密码");
            }
        }

        /// <summary>
        /// 获取安全配置摘要
        /// </summary>
        [HttpGet("configuration/summary")]
        public async Task<ActionResult<SecuritySummaryDto>> GetSecuritySummary()
        {
            try
            {
                var validationResult = await _securityValidator.ValidateConfigurationAsync();
                
                var summary = new SecuritySummaryDto
                {
                    OverallStatus = validationResult.IsValid ? "正常" : "需要修复",
                    CriticalIssuesCount = validationResult.Issues.Count(i => i.Level == SecurityValidationLevel.Critical),
                    HighIssuesCount = validationResult.Issues.Count(i => i.Level == SecurityValidationLevel.High),
                    MediumIssuesCount = validationResult.Issues.Count(i => i.Level == SecurityValidationLevel.Medium),
                    LowIssuesCount = validationResult.Issues.Count(i => i.Level == SecurityValidationLevel.Low),
                    LastCheckedAt = DateTime.UtcNow,
                    Recommendations = GetSecurityRecommendations(validationResult)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取安全配置摘要");
            }
        }

        private static List<string> GetSecurityRecommendations(SecurityValidationResult result)
        {
            var recommendations = new List<string>();

            if (result.Issues.Any(i => i.Level == SecurityValidationLevel.Critical))
            {
                recommendations.Add("立即修复关键安全问题");
            }

            if (result.Issues.Any(i => i.Level == SecurityValidationLevel.High))
            {
                recommendations.Add("尽快修复高优先级安全问题");
            }

            if (result.Issues.Any(i => i.Message.Contains("JWT")))
            {
                recommendations.Add("检查JWT配置的安全性");
            }

            if (result.Issues.Any(i => i.Message.Contains("CORS")))
            {
                recommendations.Add("审查CORS策略配置");
            }

            if (result.Issues.Any(i => i.Message.Contains("密码")))
            {
                recommendations.Add("强化密码策略");
            }

            return recommendations;
        }
    }

    // DTO类
    public class SecurityConfigurationValidationDto
    {
        public bool IsValid { get; set; }
        public bool HasWarnings { get; set; }
        public List<SecurityIssueDto> Issues { get; set; } = new();
    }

    public class SecurityIssueDto
    {
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class PasswordValidationDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class GeneratedPasswordDto
    {
        public string Password { get; set; } = string.Empty;
        public int Length { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SecuritySummaryDto
    {
        public string OverallStatus { get; set; } = string.Empty;
        public int CriticalIssuesCount { get; set; }
        public int HighIssuesCount { get; set; }
        public int MediumIssuesCount { get; set; }
        public int LowIssuesCount { get; set; }
        public DateTime LastCheckedAt { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    // Request类
    public class ValidatePasswordRequest
    {
        public string Password { get; set; } = string.Empty;
        public string? Username { get; set; }
    }

    public class GeneratePasswordRequest
    {
        public int Length { get; set; } = 16;
    }
}