using Asp.Versioning;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 统一日志管理API控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class UnifiedLogsController : ControllerBase
    {
        private readonly IUnifiedLogService _logService;
        private readonly ILogger<UnifiedLogsController> _logger;

        public UnifiedLogsController(IUnifiedLogService logService, ILogger<UnifiedLogsController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询日志
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>分页日志结果</returns>
        [HttpPost("query")]
        public async Task<ActionResult<PaginatedResult<LogDto>>> GetLogs([FromBody] LogQueryDto queryDto)
        {
            try
            {
                var result = await _logService.GetLogsAsync(queryDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询日志失败");
                return StatusCode(500, "查询日志失败");
            }
        }

        /// <summary>
        /// 根据ID获取日志详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>日志详情</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<LogDto>> GetLog(Guid id)
        {
            try
            {
                var log = await _logService.GetLogByIdAsync(id);
                if (log == null)
                {
                    return NotFound("日志不存在");
                }
                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日志详情失败: {LogId}", id);
                return StatusCode(500, "获取日志详情失败");
            }
        }

        /// <summary>
        /// 创建操作日志
        /// </summary>
        /// <param name="logCreateDto">日志创建对象</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<ActionResult> CreateLog([FromBody] LogCreateDto logCreateDto)
        {
            try
            {
                var result = await _logService.CreateLogAsync(logCreateDto);
                if (result)
                {
                    return Ok(new { Message = "日志创建成功" });
                }
                return BadRequest("日志创建失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建日志失败");
                return StatusCode(500, "创建日志失败");
            }
        }

        /// <summary>
        /// 批量创建日志
        /// </summary>
        /// <param name="logCreateDtos">日志创建对象列表</param>
        /// <returns>创建结果</returns>
        [HttpPost("batch")]
        public async Task<ActionResult> CreateLogs([FromBody] List<LogCreateDto> logCreateDtos)
        {
            try
            {
                var result = await _logService.CreateLogsAsync(logCreateDtos);
                if (result)
                {
                    return Ok(new { Message = $"成功创建 {logCreateDtos.Count} 条日志" });
                }
                return BadRequest("批量创建日志失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量创建日志失败");
                return StatusCode(500, "批量创建日志失败");
            }
        }

        /// <summary>
        /// 删除过期日志
        /// </summary>
        /// <param name="beforeDate">删除此日期之前的日志</param>
        /// <returns>删除结果</returns>
        [HttpDelete("expired")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteExpiredLogs([FromQuery] DateTime beforeDate)
        {
            try
            {
                var deletedCount = await _logService.DeleteExpiredLogsAsync(beforeDate);
                return Ok(new { Message = $"成功删除 {deletedCount} 条过期日志" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除过期日志失败");
                return StatusCode(500, "删除过期日志失败");
            }
        }

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>统计信息</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<Dictionary<string, object>>> GetLogStatistics(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var statistics = await _logService.GetLogStatisticsAsync(startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日志统计失败");
                return StatusCode(500, "获取日志统计失败");
            }
        }

        /// <summary>
        /// 获取用户操作统计
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>用户操作统计</returns>
        [HttpGet("user-statistics/{userId}")]
        public async Task<ActionResult<Dictionary<string, object>>> GetUserActionStatistics(
            Guid userId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var statistics = await _logService.GetUserActionStatisticsAsync(userId, startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户操作统计失败");
                return StatusCode(500, "获取用户操作统计失败");
            }
        }

        /// <summary>
        /// 导出日志到CSV
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>CSV文件</returns>
        [HttpPost("export/csv")]
        public async Task<ActionResult> ExportLogsToCsv([FromBody] LogQueryDto queryDto)
        {
            try
            {
                var csvData = await _logService.ExportLogsToCsvAsync(queryDto);
                return File(csvData, "text/csv", $"logs_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出日志到CSV失败");
                return StatusCode(500, "导出日志失败");
            }
        }

        /// <summary>
        /// 导出日志到Excel
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>Excel文件</returns>
        [HttpPost("export/excel")]
        public async Task<ActionResult> ExportLogsToExcel([FromBody] LogQueryDto queryDto)
        {
            try
            {
                var excelData = await _logService.ExportLogsToExcelAsync(queryDto);
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"logs_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出日志到Excel失败");
                return StatusCode(500, "导出日志失败");
            }
        }

        /// <summary>
        /// 记录用户登录日志
        /// </summary>
        /// <param name="request">登录日志请求</param>
        /// <returns>记录结果</returns>
        [HttpPost("user-login")]
        public async Task<ActionResult> LogUserLogin([FromBody] UserLoginLogRequest request)
        {
            try
            {
                await _logService.LogUserLoginAsync(
                    request.UserId,
                    request.Username,
                    request.ClientIP,
                    request.UserAgent,
                    request.IsSuccess,
                    request.ErrorMessage);

                return Ok(new { Message = "登录日志记录成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户登录日志失败");
                return StatusCode(500, "记录登录日志失败");
            }
        }

        /// <summary>
        /// 记录用户登出日志
        /// </summary>
        /// <param name="request">登出日志请求</param>
        /// <returns>记录结果</returns>
        [HttpPost("user-logout")]
        public async Task<ActionResult> LogUserLogout([FromBody] UserLogoutLogRequest request)
        {
            try
            {
                await _logService.LogUserLogoutAsync(request.UserId, request.Username, request.ClientIP);
                return Ok(new { Message = "登出日志记录成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户登出日志失败");
                return StatusCode(500, "记录登出日志失败");
            }
        }
    }

    /// <summary>
    /// 用户登录日志请求模型
    /// </summary>
    public class UserLoginLogRequest
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 用户登出日志请求模型
    /// </summary>
    public class UserLogoutLogRequest
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
    }
}