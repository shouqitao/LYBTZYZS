using LYBT.Common.Enums;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Sync.Dtos;
using LYBT.Module.Sync.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Sync.Controllers {

    /// <summary>
    /// 数据同步任务与日志 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示SyncController。
/// </summary>
public class SyncController : ControllerBase {
        private readonly ISyncService _syncService;
        private readonly IGlobalSettingsService _settingsService;

        /// <summary>
        /// 构造方法，注入同步服务
        /// </summary>
        public SyncController(ISyncService syncService, IGlobalSettingsService settingsService) {
            _syncService = syncService;
            _settingsService = settingsService;
        }

        // ================= 日志相关 ===================
        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        [HttpGet("logs")]
/// <summary>
/// 执行GetLogList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<SyncLogDto>>> GetLogList() {
            var list = await _syncService.GetLogListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取最近一次同步信息
        /// </summary>
        [HttpGet("logs/last")]
/// <summary>
/// 执行GetLastLog操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<SyncLogDto?>> GetLastLog() {
            var info = await _syncService.GetLastSyncInfoAsync();
            return Ok(info);
        }

        /// <summary>
        /// 分页查询同步日志
        /// </summary>
        [HttpGet("logs/paged")]
/// <summary>
/// 执行GetLogPaged操作。
/// </summary>
/// <param name="1">参数1</param>
/// <param name="20">参数20</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<SyncLogDto>>> GetLogPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
            var list = await _syncService.GetSyncLogPagedAsync(page, pageSize);
            return Ok(list);
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        [HttpPost("logs")]
/// <summary>
/// 执行AddLog操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> AddLog([FromBody] SyncLogCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _syncService.AddLogAsync(dto);
            if (!result)
                return BadRequest("新增同步日志失败");
            return Ok("新增同步日志成功");
        }

        /// <summary>
        /// 删除同步日志
        /// </summary>
        [HttpDelete("logs/{id}")]
/// <summary>
/// 执行DeleteLog操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> DeleteLog(string id) {
            var result = await _syncService.DeleteLogAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除同步日志成功");
        }

        /// <summary>
        /// 检测中心数据库是否可连接
        /// </summary>
        [HttpGet("connection-status")]
/// <summary>
/// 执行CheckConnection操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<bool>> CheckConnection() {
            var can = await _syncService.CheckConnectionStatusAsync();
            return Ok(can);
        }

        /// <summary>
        /// 手动触发同步
        /// </summary>
        [HttpPost("manual-sync")]
/// <summary>
/// 执行ManualSync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult> ManualSync() {
            var result = await _syncService.TriggerManualSyncAsync();
            if (!result)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// 获取当前同步模式
        /// </summary>
        [HttpGet("mode")]
/// <summary>
/// 执行GetSyncMode操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<SyncMode>> GetSyncMode() {
            var settings = await _settingsService.GetAsync();
            return Ok(settings?.SyncMode ?? SyncMode.Auto);
        }

        /// <summary>
        /// 设置同步模式
        /// </summary>
        [HttpPost("mode")]
/// <summary>
/// 执行SetSyncMode操作。
/// </summary>
/// <param name="mode">参数mode</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> SetSyncMode([FromBody] SyncMode mode) {
            var settings = await _settingsService.GetAsync() ?? new Settings.Dtos.GlobalSettingsDto();
            settings.SyncMode = mode;
            var result = await _settingsService.SaveAsync(settings);
            if (!result)
                return BadRequest();
            return Ok();
        }

        // ================ 任务相关 ===================
        /// <summary>
        /// 获取同步任务列表
        /// </summary>
        [HttpGet("tasks")]
/// <summary>
/// 执行GetTaskList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<SyncTaskDto>>> GetTaskList() {
            var list = await _syncService.GetTaskListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取同步任务详情
        /// </summary>
        [HttpGet("tasks/{id}")]
/// <summary>
/// 执行GetTaskDetail操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<SyncTaskDetailDto>> GetTaskDetail(Guid id) {
            var detail = await _syncService.GetTaskDetailAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增同步任务
        /// </summary>
        [HttpPost("tasks")]
/// <summary>
/// 执行AddTask操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> AddTask([FromBody] SyncTaskCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _syncService.AddTaskAsync(dto);
            if (!result)
                return BadRequest("新增同步任务失败");
            return Ok("新增同步任务成功");
        }

        /// <summary>
        /// 更新同步任务
        /// </summary>
        [HttpPut("tasks")]
/// <summary>
/// 执行UpdateTask操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> UpdateTask([FromBody] SyncTaskEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _syncService.UpdateTaskAsync(dto);
            if (!result)
                return BadRequest("更新同步任务失败");
            return Ok("更新同步任务成功");
        }

        /// <summary>
        /// 删除同步任务
        /// </summary>
        [HttpDelete("tasks/{id}")]
/// <summary>
/// 执行DeleteTask操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> DeleteTask(Guid id) {
            var result = await _syncService.DeleteTaskAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除同步任务成功");
        }
    }
}
