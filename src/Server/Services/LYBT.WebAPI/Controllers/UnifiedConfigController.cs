using Asp.Versioning;
using LYBT.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 统一配置管理API控制器 - UltraThink简化版
    /// 专注小诊所实际需求，移除复杂的全局设置、诊断目录管理等功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UnifiedConfigController : ControllerBase
    {
        private readonly ISimplifiedConfigurationService _configService;
        private readonly ILogger<UnifiedConfigController> _logger;

        public UnifiedConfigController(ISimplifiedConfigurationService configService, ILogger<UnifiedConfigController> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        // ==================== 基础配置管理 ====================

        /// <summary>
        /// 获取环境信息
        /// </summary>
        /// <returns>环境信息</returns>
        [HttpGet("environment")]
        public ActionResult GetEnvironment()
        {
            try
            {
                var environment = new
                {
                    IsDevelopment = _configService.IsDevelopment,
                    IsProduction = _configService.IsProduction,
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(new { Message = "环境信息获取成功", Data = environment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取环境信息失败");
                return StatusCode(500, "获取环境信息失败");
            }
        }

        /// <summary>
        /// 获取配置节信息
        /// </summary>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>配置信息</returns>
        [HttpGet("section/{sectionName}")]
        public ActionResult GetConfigSection(string sectionName)
        {
            try
            {
                // 根据配置节名称返回相应配置
                switch (sectionName.ToLower())
                {
                    case "database":
                        var dbConfig = new
                        {
                            ConnectionString = _configService.GetConnectionString(),
                            IsConfigured = !string.IsNullOrEmpty(_configService.GetConnectionString())
                        };
                        return Ok(new { Message = "数据库配置获取成功", Data = dbConfig });

                    case "jwt":
                        var jwtConfig = new
                        {
                            IsConfigured = !string.IsNullOrEmpty(_configService.GetJwtSecret()),
                            SecretLength = _configService.GetJwtSecret()?.Length ?? 0
                        };
                        return Ok(new { Message = "JWT配置获取成功", Data = jwtConfig });

                    default:
                        return BadRequest($"不支持的配置节: {sectionName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取配置节失败: {SectionName}", sectionName);
                return StatusCode(500, "获取配置节失败");
            }
        }

        /// <summary>
        /// 验证配置完整性
        /// </summary>
        /// <returns>配置验证结果</returns>
        [HttpGet("validate")]
        public ActionResult ValidateConfiguration()
        {
            try
            {
                var validationResult = new
                {
                    DatabaseConnection = !string.IsNullOrEmpty(_configService.GetConnectionString()),
                    JwtSecret = !string.IsNullOrEmpty(_configService.GetJwtSecret()),
                    AdminPassword = !string.IsNullOrEmpty(_configService.GetAdminPassword()),
                    UserPassword = !string.IsNullOrEmpty(_configService.GetUserDefaultPassword()),
                    Environment = _configService.IsDevelopment ? "Development" : 
                                _configService.IsProduction ? "Production" : "Unknown",
                    ValidationTime = DateTime.UtcNow
                };

                var isValid = validationResult.DatabaseConnection && 
                             validationResult.JwtSecret && 
                             validationResult.AdminPassword && 
                             validationResult.UserPassword;

                return Ok(new { 
                    Message = isValid ? "配置验证通过" : "配置验证失败",
                    IsValid = isValid,
                    Data = validationResult 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置验证失败");
                return StatusCode(500, "配置验证失败");
            }
        }

        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        /// <returns>系统状态</returns>
        [HttpGet("status")]
        [Authorize(Roles = "Admin")]
        public ActionResult GetSystemStatus()
        {
            try
            {
                var status = new
                {
                    ApplicationName = "凌隐宝堂中医诊所诊疗系统",
                    Version = "UltraThink v2.0",
                    StartTime = DateTime.UtcNow, // 简化版本，实际应该从启动时间计算
                    Environment = _configService.IsDevelopment ? "Development" : 
                                _configService.IsProduction ? "Production" : "Unknown",
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = $"{Environment.WorkingSet / 1024 / 1024} MB",
                    GCMemory = $"{GC.GetTotalMemory(false) / 1024 / 1024} MB",
                    ThreadCount = Environment.ProcessorCount,
                    Configuration = new
                    {
                        DatabaseConnected = !string.IsNullOrEmpty(_configService.GetConnectionString()),
                        JwtConfigured = !string.IsNullOrEmpty(_configService.GetJwtSecret()),
                        AdminConfigured = !string.IsNullOrEmpty(_configService.GetAdminPassword())
                    }
                };

                return Ok(new { Message = "系统状态获取成功", Data = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统状态失败");
                return StatusCode(500, "获取系统状态失败");
            }
        }

        // UltraThink v2.0简化版配置控制器 - 专注于基础配置验证和环境信息
        // 移除复杂的全局设置管理、系统设置管理、诊断目录管理、治疗目录管理等功能
        // 专注小诊所实际需求：基础配置验证、环境信息、系统状态
        // 如需复杂配置管理功能，可在后续版本根据用户实际需求添加
    }
}