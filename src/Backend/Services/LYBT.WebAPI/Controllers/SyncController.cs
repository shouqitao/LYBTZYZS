using Asp.Versioning;
using LYBT.Infrastructure.Configuration;
using LYBT.Module.Sync.Interfaces;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 数据同步任务与日志 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class SyncController : BaseController {
        private readonly ISyncService _syncService;
        private readonly IUnifiedConfigService _configService;

        /// <summary>
        /// 构造方法，注入同步服务
        /// </summary>
        public SyncController(ISyncService syncService, IUnifiedConfigService configService, IMemoryCache cache, ILogger<SyncController> logger) 
            : base(logger, cache) {
            _syncService = syncService;
            _configService = configService;
        }

        // ================= 日志相关 ===================
        /// <summary>
        /// 获取所有同步日志
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<List<SyncLogDto>>> GetLogList() {
            try {
                var list = await _syncService.GetLogListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取同步日志列表");
            }
        }

        /// <summary>
        /// 获取最近一次同步信息
        /// </summary>
        [HttpGet("logs/last")]
        public async Task<ActionResult<SyncLogDto?>> GetLastLog() {
            try {
                var info = await _syncService.GetLastSyncInfoAsync();
                return Ok(info);
            } catch (Exception ex) {
                return HandleException(ex, "获取最近一次同步信息");
            }
        }

        /// <summary>
        /// 分页查询同步日志
        /// </summary>
        [HttpGet("logs/paged")]
        public async Task<ActionResult<List<SyncLogDto>>> GetLogPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20) {
            try {
                var list = await _syncService.GetSyncLogPagedAsync(page, pageSize);
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "分页查询同步日志");
            }
        }

        /// <summary>
        /// 新增同步日志
        /// </summary>
        [HttpPost("logs")]
        public async Task<ActionResult<object>> AddLog([FromBody] SyncLogCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _syncService.AddLogAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增同步日志失败" });
                }

                LogOperation("新增同步日志成功", dto, null);
                return Ok(new { message = "新增同步日志成功" });
            } catch (Exception ex) {
                return HandleException(ex, "新增同步日志");
            }
        }

        /// <summary>
        /// 删除同步日志
        /// </summary>
        [HttpDelete("logs/{id}")]
        public async Task<ActionResult<object>> DeleteLog(string id) {
            try {
                if (string.IsNullOrEmpty(id)) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "日志ID不能为空" });
                }

                var result = await _syncService.DeleteLogAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "同步日志不存在" });
                }

                LogOperation("删除同步日志成功", new { LogId = id }, null);
                return Ok(new { message = "删除同步日志成功" });
            } catch (Exception ex) {
                return HandleException(ex, "删除同步日志", new { LogId = id });
            }
        }

        /// <summary>
        /// 检测中心数据库是否可连接
        /// </summary>
        [HttpGet("connection-status")]
        public async Task<ActionResult<bool>> CheckConnection() {
            try {
                var can = await _syncService.CheckConnectionStatusAsync();
                return Ok(can);
            } catch (Exception ex) {
                return HandleException(ex, "检测中心数据库连接");
            }
        }

        /// <summary>
        /// 手动触发同步
        /// </summary>
        [HttpPost("manual-sync")]
        public async Task<ActionResult<object>> ManualSync() {
            try {
                var result = await _syncService.TriggerManualSyncAsync();
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "手动触发同步失败" });
                }

                LogOperation("手动触发同步成功", null, null);
                return Ok(new { message = "手动同步触发成功" });
            } catch (Exception ex) {
                return HandleException(ex, "手动触发同步");
            }
        }

        /// <summary>
        /// 获取当前同步模式
        /// </summary>
        [HttpGet("mode")]
        public async Task<ActionResult<SyncMode>> GetSyncMode() {
            try {
                var syncMode = await _configService.GetSettingAsync<SyncMode>("SyncMode", SyncMode.Auto);
                return Ok(syncMode);
            } catch (Exception ex) {
                return HandleException(ex, "获取同步模式");
            }
        }

        /// <summary>
        /// 设置同步模式
        /// </summary>
        [HttpPost("mode")]
        public async Task<ActionResult<object>> SetSyncMode([FromBody] SyncMode mode) {
            try {
                await _configService.SetSettingAsync("SyncMode", mode, "系统同步模式", "Sync");
                LogOperation("设置同步模式", new { Mode = mode }, null);
                return Ok(new { message = "同步模式设置成功" });
            } catch (Exception ex) {
                return HandleException(ex, "设置同步模式", new { Mode = mode });
            }
        }

        // ================ 任务相关 ===================
        /// <summary>
        /// 获取同步任务列表
        /// </summary>
        [HttpGet("tasks")]
        public async Task<ActionResult<List<SyncTaskDto>>> GetTaskList() {
            try {
                var list = await _syncService.GetTaskListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取同步任务列表");
            }
        }

        /// <summary>
        /// 获取同步任务详情
        /// </summary>
        [HttpGet("tasks/{id}")]
        public async Task<ActionResult<SyncTaskDetailDto>> GetTaskDetail(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "同步任务ID");
                if (validationResult != null) return validationResult;

                var detail = await _syncService.GetTaskDetailAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "同步任务不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取同步任务详情", new { TaskId = id });
            }
        }

        /// <summary>
        /// 新增同步任务
        /// </summary>
        [HttpPost("tasks")]
        public async Task<ActionResult<object>> AddTask([FromBody] SyncTaskCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _syncService.AddTaskAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增同步任务失败" });
                }

                LogOperation("新增同步任务成功", dto, null);
                return Ok(new { message = "新增同步任务成功" });
            } catch (Exception ex) {
                return HandleException(ex, "新增同步任务");
            }
        }

        /// <summary>
        /// 更新同步任务
        /// </summary>
        [HttpPut("tasks")]
        public async Task<ActionResult<object>> UpdateTask([FromBody] SyncTaskEditDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _syncService.UpdateTaskAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "更新同步任务失败" });
                }

                LogOperation("更新同步任务成功", dto, dto.Id);
                return Ok(new { message = "更新同步任务成功" });
            } catch (Exception ex) {
                return HandleException(ex, "更新同步任务", new { TaskId = dto.Id });
            }
        }

        /// <summary>
        /// 删除同步任务
        /// </summary>
        [HttpDelete("tasks/{id}")]
        public async Task<ActionResult<object>> DeleteTask(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "同步任务ID");
                if (validationResult != null) return validationResult;

                var result = await _syncService.DeleteTaskAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "同步任务不存在" });
                }

                LogOperation("删除同步任务成功", null, id);
                return Ok(new { message = "删除同步任务成功" });
            } catch (Exception ex) {
                return HandleException(ex, "删除同步任务", new { TaskId = id });
            }
        }
    }
}