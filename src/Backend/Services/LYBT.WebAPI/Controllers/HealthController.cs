using LYBT.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 系统健康检查控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase {
        private readonly DatabaseInitializationService _dbInitService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(DatabaseInitializationService dbInitService, ILogger<HealthController> logger) {
            _dbInitService = dbInitService;
            _logger = logger;
        }

        /// <summary>
        /// 基本健康检查
        /// </summary>
        [HttpGet]
        public string Get() {
            return "Healthy - LYBT中医诊所管理系统API";
        }

        /// <summary>
        /// 数据库健康检查
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> CheckDatabase() {
            try {
                var dbInfo = await _dbInitService.GetDatabaseInfoAsync();

                return Ok(new {
                    Status = dbInfo.IsConnected ? "Healthy" : "Unhealthy",
                    DatabaseName = dbInfo.DatabaseName,
                    IsConnected = dbInfo.IsConnected,
                    AppliedMigrations = dbInfo.AppliedMigrationsCount,
                    PendingMigrations = dbInfo.PendingMigrationsCount,
                    LastMigration = dbInfo.LastMigration,
                    CheckTime = DateTime.UtcNow
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "数据库健康检查失败");
                return StatusCode(500, new {
                    Status = "Unhealthy",
                    Error = ex.Message,
                    CheckTime = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// 详细系统状态
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailedStatus() {
            try {
                var dbInfo = await _dbInitService.GetDatabaseInfoAsync();

                return Ok(new {
                    System = new {
                        Status = "Running",
                        StartTime = Process.GetCurrentProcess().StartTime,
                        Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                        MachineName = Environment.MachineName,
                        ProcessorCount = Environment.ProcessorCount,
                        OSVersion = Environment.OSVersion.ToString()
                    },
                    Database = new {
                        Status = dbInfo.IsConnected ? "Connected" : "Disconnected",
                        DatabaseName = dbInfo.DatabaseName,
                        AppliedMigrations = dbInfo.AppliedMigrationsCount,
                        PendingMigrations = dbInfo.PendingMigrationsCount,
                        LastMigration = dbInfo.LastMigration
                    },
                    Memory = new {
                        WorkingSet = GC.GetTotalMemory(false),
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2)
                    },
                    CheckTime = DateTime.UtcNow
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "获取详细系统状态失败");
                return StatusCode(500, new {
                    Status = "Error",
                    Error = ex.Message,
                    CheckTime = DateTime.UtcNow
                });
            }
        }
    }
}