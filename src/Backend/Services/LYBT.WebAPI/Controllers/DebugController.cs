using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 调试控制器 - 用于诊断数据库连接和映射问题
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
                return Ok(ApiResponse<object>.Success(new 
                { 
                    CanConnect = canConnect,
                    ConnectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "..."
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接测试失败");
                return StatusCode(500, ApiResponse<object>.Fail($"数据库连接失败: {ex.Message}", 500));
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
                    .Select(u => new { 
                        u.Id, 
                        u.Username, 
                        u.RealName, 
                        u.CreateTime,
                        u.IsActive 
                    })
                    .ToListAsync();
                
                _logger.LogInformation($"成功查询到 {users.Count} 个用户");
                
                return Ok(ApiResponse<object>.Success(new {
                    TotalCount = userCount,
                    Users = users
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户表查询失败");
                return StatusCode(500, ApiResponse<object>.Fail($"用户表查询失败: {ex.Message}", 500));
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
                
                var patients = await _context.Patients
                    .Take(5)
                    .Select(p => new { 
                        p.Id, 
                        p.Name, 
                        p.CreateTime,
                        p.IsActive 
                    })
                    .ToListAsync();
                
                return Ok(ApiResponse<object>.Success(new {
                    TotalCount = patientCount,
                    Patients = patients
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "患者表查询失败");
                return StatusCode(500, ApiResponse<object>.Fail($"患者表查询失败: {ex.Message}", 500));
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
                
                var herbs = await _context.Herbs
                    .Take(5)
                    .Select(h => new { 
                        h.Id, 
                        h.Name, 
                        h.CreateTime,
                        h.IsActive 
                    })
                    .ToListAsync();
                
                return Ok(ApiResponse<object>.Success(new {
                    TotalCount = herbCount,
                    Herbs = herbs
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "药材表查询失败");
                return StatusCode(500, ApiResponse<object>.Fail($"药材表查询失败: {ex.Message}", 500));
            }
        }
    }
}