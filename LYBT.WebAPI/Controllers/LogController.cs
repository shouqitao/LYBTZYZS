using LYBT.Common.Enums.Users;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 操作日志Web API控制器，提供日志写入与查询接口
/// </summary>
[ApiController]
[ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
/// <summary>
/// 表示LogController。
/// </summary>
public class LogController : ControllerBase {
    private readonly ILogService _logService;

    /// <summary>
    /// 构造方法，注入日志服务
    /// </summary>
    public LogController(ILogService logService) {
        _logService = logService;
    }

    /// <summary>
    /// 写入一条操作日志（所有业务模块通用）
    /// </summary>
    /// <param name="logDto">日志DTO</param>
    /// <returns>写入后的日志ID</returns>
    [HttpPost]
/// <summary>
/// 执行AddLog操作。
/// </summary>
/// <param name="logDto">参数logDto</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> AddLog([FromBody] LogDto logDto) {
        var id = await _logService.AddLogAsync(logDto);
        if (id == Guid.Empty)
            return BadRequest(new { success = false, message = "日志写入失败" });
        return Ok(new { success = true, id });
    }

    /// <summary>
    /// 分页/条件查询操作日志
    /// </summary>
    /// <param name="query">日志查询条件</param>
    /// <returns>日志分页结果</returns>
    [HttpGet]
/// <summary>
/// 执行GetLogs操作。
/// </summary>
/// <param name="query">参数query</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetLogs([FromQuery] LogQueryDto query) {
        // 实际项目应从登录上下文获取角色与ID
        UserRole role = UserRole.Admin;
        Guid userId = Guid.NewGuid();
        var (logs, total) = await _logService.GetLogsAsync(query, role, userId);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定用户的操作日志
    /// </summary>
    [HttpGet("user/{userId}")]
/// <summary>
/// 执行GetUserLogs操作。
/// </summary>
/// <param name="userId">参数userId</param>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetUserLogs(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        UserRole role = UserRole.Admin;
        Guid currentUserId = Guid.NewGuid();
        var query = new LogQueryDto {
            ObjectType = LYBT.Common.Enums.Logs.ObjectType.User,
            ObjectId = userId,
            Page = page,
            PageSize = pageSize
        };
        var (logs, total) = await _logService.GetLogsAsync(query, role, currentUserId);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定患者的操作日志
    /// </summary>
    [HttpGet("patient/{patientId}")]
/// <summary>
/// 执行GetPatientLogs操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetPatientLogs(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        UserRole role = UserRole.Admin;
        Guid currentUserId = Guid.NewGuid();
        var query = new LogQueryDto {
            ObjectType = LYBT.Common.Enums.Logs.ObjectType.Patient,
            ObjectId = patientId,
            Page = page,
            PageSize = pageSize
        };
        var (logs, total) = await _logService.GetLogsAsync(query, role, currentUserId);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定病历的操作日志
    /// </summary>
    [HttpGet("record/{recordId}")]
/// <summary>
/// 执行GetRecordLogs操作。
/// </summary>
/// <param name="recordId">参数recordId</param>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetRecordLogs(Guid recordId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        UserRole role = UserRole.Admin;
        Guid currentUserId = Guid.NewGuid();
        var query = new LogQueryDto {
            ObjectType = LYBT.Common.Enums.Logs.ObjectType.Record,
            ObjectId = recordId,
            Page = page,
            PageSize = pageSize
        };
        var (logs, total) = await _logService.GetLogsAsync(query, role, currentUserId);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定处方的操作日志
    /// </summary>
    [HttpGet("prescription/{prescriptionId}")]
/// <summary>
/// 执行GetPrescriptionLogs操作。
/// </summary>
/// <param name="prescriptionId">参数prescriptionId</param>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetPrescriptionLogs(Guid prescriptionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        UserRole role = UserRole.Admin;
        Guid currentUserId = Guid.NewGuid();
        var query = new LogQueryDto {
            ObjectType = LYBT.Common.Enums.Logs.ObjectType.Prescription,
            ObjectId = prescriptionId,
            Page = page,
            PageSize = pageSize
        };
        var (logs, total) = await _logService.GetLogsAsync(query, role, currentUserId);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取日志详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <returns>日志详情DTO</returns>
    [HttpGet("{id}")]
/// <summary>
/// 执行GetLogById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
    public async Task<IActionResult> GetLogById(Guid id) {
        UserRole role = UserRole.Admin;
        Guid currentUserId = Guid.NewGuid();
        var log = await _logService.GetLogByIdAsync(id);
        if (log == null)
            return NotFound(new { success = false, message = "日志不存在" });
        if (role != UserRole.Admin && log.OperatorId != currentUserId)
            return Forbid();
        return Ok(log);
    }
}
