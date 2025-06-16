using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Dtos;

namespace LYBT.Module.Sync.Controllers {
    /// <summary>
    /// 数据同步任务与日志 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase {
        private readonly ISyncService _syncService;

        /// <summary>
        /// 构造方法，注入同步服务
        /// </summary>
        public SyncController(ISyncService syncService) {
            _syncService = syncService;
        }

        // ================= 日志相关 ===================
        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<List<SyncLogDto>>> GetLogList() {
            var list = await _syncService.GetLogListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        [HttpPost("logs")]
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
        public async Task<ActionResult> DeleteLog(Guid id) {
            var result = await _syncService.DeleteLogAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除同步日志成功");
        }

        // ================ 任务相关 ===================
        /// <summary>
        /// 获取同步任务列表
        /// </summary>
        [HttpGet("tasks")]
        public async Task<ActionResult<List<SyncTaskDto>>> GetTaskList() {
            var list = await _syncService.GetTaskListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取同步任务详情
        /// </summary>
        [HttpGet("tasks/{id}")]
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
        public async Task<ActionResult> DeleteTask(Guid id) {
            var result = await _syncService.DeleteTaskAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除同步任务成功");
        }
    }
}
