using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LYBT.LocalWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous]
    public class HealthController : BaseApiController
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db, ILogger<HealthController> logger)
            : base(logger)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/health — 基础健康检查
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            bool canConnect = false;
            try
            {
                canConnect = await _db.Database.CanConnectAsync();
            }
            catch
            {
                canConnect = false;
            }

            var status = canConnect ? "Healthy" : "Degraded";
            var result = new
            {
                status = status,
                timestamp = DateTime.UtcNow,
                database = canConnect ? "Connected" : "Disconnected"
            };
            return Success(result);
        }

        /// <summary>
        /// GET /api/health/ping — 简单存活检查
        /// </summary>
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Success(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// GET /api/health/details — 详细健康信息（DB 连接、版本等）
        /// </summary>
        [HttpGet("details")]
        [Authorize]
        public async Task<IActionResult> GetDetails()
        {
            var dbConnected = false;
            var dbVersion = "unknown";
            var dbResponseMs = 0L;

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                dbConnected = await _db.Database.CanConnectAsync();
                sw.Stop();
                dbResponseMs = sw.ElapsedMilliseconds;

                if (dbConnected)
                {
                    dbVersion = _db.Database.ProviderName ?? "unknown";
                }
            }
            catch (Exception ex)
            {
                dbConnected = false;
                dbVersion = $"Error: {ex.Message}";
            }

            var userCount = 0;
            try
            {
                userCount = await _db.Users.IgnoreQueryFilters().CountAsync();
            }
            catch
            {
                // ignore
            }

            return Success(new
            {
                status = dbConnected ? "Healthy" : "Degraded",
                timestamp = DateTime.UtcNow,
                version = "1.0.0-local",
                database = new
                {
                    connected = dbConnected,
                    provider = dbVersion,
                    responseMs = dbResponseMs
                },
                statistics = new
                {
                    totalUsers = userCount
                }
            });
        }
    }
}
