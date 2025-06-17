using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

/// <summary>
/// 操作日志Web API控制器，提供日志写入与查询接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetLogs([FromQuery] LogQueryDto query) {
        var (logs, total) = await _logService.GetLogsAsync(query);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定用户的操作日志
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserLogs(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var (logs, total) = await _logService.GetUserLogsAsync(userId, page, pageSize);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取指定患者的操作日志
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientLogs(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
        var (logs, total) = await _logService.GetPatientLogsAsync(patientId, page, pageSize);
        return Ok(new { total, logs });
    }

    /// <summary>
    /// 获取日志详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <returns>日志详情DTO</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogById(Guid id) {
        var log = await _logService.GetLogByIdAsync(id);
        if (log == null)
            return NotFound(new { success = false, message = "日志不存在" });
        return Ok(log);
    }
}
