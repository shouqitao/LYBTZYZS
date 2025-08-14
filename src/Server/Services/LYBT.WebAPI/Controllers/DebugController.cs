using Asp.Versioning;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 调试控制器 - 用于诊断数据库连接和映射问题
    /// 注意：此控制器仅应在开发环境中启用
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [AllowAnonymous] // 临时允许匿名访问以便调试
    public class DebugController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DebugController> _logger;

        public DebugController(AppDbContext context, ILogger<DebugController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return Ok(new
                {
                    canConnect = canConnect,
                    connectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "..."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接测试失败");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "数据库连接失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 测试用户表查询
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> TestUsers()
        {
            try
            {
                _logger.LogInformation("开始测试用户表查询");

                // 直接查询数据库
                var userCount = await _context.Users.CountAsync();
                _logger.LogInformation($"用户表中有 {userCount} 条记录");

                // 尝试获取前5个用户
                var users = await _context.Users
                    .Take(5)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.RealName,
                        u.CreateTime,
                        Status = u.Status == CommonStatus.Enabled
                    })
                    .ToListAsync();

                _logger.LogInformation($"成功查询到 {users.Count} 个用户");

                return Ok(new
                {
                    totalCount = userCount,
                    users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户表查询失败");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "用户表查询失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 测试患者表查询
        /// </summary>
        [HttpGet("patients")]
        public async Task<IActionResult> TestPatients()
        {
            try
            {
                _logger.LogInformation("开始测试患者表查询");

                var patientCount = await _context.Patients.CountAsync();
                _logger.LogInformation($"患者表中有 {patientCount} 条记录");

                // 使用EF查询，现在应该不会有字段映射问题
                object patients;
                if (patientCount > 0)
                {
                    patients = await _context.Patients
                        .Take(5)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.Gender,
                            p.CreateTime,
                            p.Status
                        })
                        .ToListAsync();
                }
                else
                {
                    patients = new object[0];
                }

                return Ok(new
                {
                    totalCount = patientCount,
                    patients = patients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "患者表查询失败");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "患者表查询失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 测试药材表查询
        /// </summary>
        [HttpGet("herbs")]
        public async Task<IActionResult> TestHerbs()
        {
            try
            {
                _logger.LogInformation("开始测试药材表查询");

                var herbCount = await _context.Herbs.CountAsync();
                _logger.LogInformation($"药材表中有 {herbCount} 条记录");

                // 使用EF查询，现在应该不会有字段映射问题
                object herbs;
                if (herbCount > 0)
                {
                    herbs = await _context.Herbs
                        .Take(5)
                        .Select(h => new
                        {
                            h.Id,
                            h.Name,
                            Status = h.Status == CommonStatus.Enabled,
                            h.Price,
                            h.Unit
                        })
                        .ToListAsync();
                }
                else
                {
                    herbs = new object[0];
                }

                return Ok(new
                {
                    totalCount = herbCount,
                    herbs = herbs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "药材表查询失败");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "药材表查询失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 检查所有表
        /// </summary>
        [HttpGet("tables")]
        public async Task<IActionResult> ListTables()
        {
            try
            {
                var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                var tables = await _context.Database.SqlQueryRaw<string>(sql).ToListAsync();

                return Ok(new
                {
                    tableCount = tables.Count,
                    tables = tables
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表列表失败");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "获取表列表失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }

        /// <summary>
        /// 检查表结构 - 查看实际存在的字段
        /// </summary>
        [HttpGet("table-structure/{tableName}")]
        public async Task<IActionResult> CheckTableStructure(string tableName)
        {
            try
            {
                // 直接尝试查询列信息
                var columnSql = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + tableName + "' ORDER BY ORDINAL_POSITION";
                var columns = await _context.Database.SqlQueryRaw<dynamic>(columnSql).ToListAsync();

                return Ok(new
                {
                    tableName = tableName,
                    columnCount = columns.Count,
                    columns = columns
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"检查表结构失败: {tableName}");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "检查表结构失败",
                    Detail = ex.Message,
                    Status = 500
                });
            }
        }
    }
}