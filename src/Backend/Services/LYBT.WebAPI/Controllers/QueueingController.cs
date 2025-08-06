using Asp.Versioning;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 排队管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class QueueingController : BaseController {
        private readonly IQueueingService _queueingService;

        /// <summary>
        /// 构造方法，注入排队服务
        /// </summary>
        public QueueingController(IQueueingService queueingService, IMemoryCache cache, ILogger<QueueingController> logger) 
            : base(logger, cache) {
            _queueingService = queueingService;
        }

        /// <summary>
        /// 获取排队列表 (RESTful GET /Queueing) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<QueueingDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] string? queueType = null,
            [FromQuery] QueueStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(queueType) && !status.HasValue &&
                    !startDate.HasValue && !endDate.HasValue) {
                    
                    var list = await _queueingService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<QueueingDto>();
                    var result = new PaginatedResult<QueueingDto> {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Ok(result);
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new LYBT.Shared.Models.Common.PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _queueingService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取排队列表");
            }
        }

        /// <summary>
        /// 分页获取排队列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<QueueingDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _queueingService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取排队列表");
            }
        }

        /// <summary>
        /// 获取排队详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<QueueingDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "排队ID");
                if (validationResult != null) return validationResult;

                var detail = await _queueingService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取排队详情", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 新增排队
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<QueueingDto>> Add([FromBody] QueueingCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _queueingService.AddAsync(dto);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "新增排队失败", Status = 400 });
                }

                LogOperation("新增排队成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增排队");
            }
        }

        /// <summary>
        /// 编辑排队
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] QueueingEditDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "编辑排队失败", Status = 400 });
                }

                // 获取更新后的资源
                var updated = await _queueingService.GetByIdAsync(dto.Id);
                LogOperation("编辑排队成功", updated, dto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑排队", new { QueueingId = dto.Id });
            }
        }

        /// <summary>
        /// 删除排队
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "排队ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                LogOperation("删除排队成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除排队", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 取消排队
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<object>> Cancel(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "排队ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CancelAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                // 获取更新后的资源
                var updated = await _queueingService.GetByIdAsync(id);
                LogOperation("取消排队成功", updated, id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "取消排队", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 完成排队
        /// </summary>
        [HttpPost("complete/{id}")]
        public async Task<ActionResult<object>> Complete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "排队ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CompleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                // 获取更新后的资源
                var updated = await _queueingService.GetByIdAsync(id);
                LogOperation("完成排队成功", updated, id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "完成排队", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 暂停排队
        /// </summary>
        [HttpPost("hold/{id}")]
        public async Task<ActionResult<object>> Hold(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "排队ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.HoldAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                LogOperation("暂停排队成功", null, id);
                return Ok(new { message = "暂停排队成功" });
            } catch (Exception ex) {
                return HandleException(ex, "暂停排队", new { QueueingId = id });
            }
        }

        #region 现场叫号功能

        /// <summary>
        /// 获取今日排队列表
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<List<QueueingDto>>> GetTodayQueues([FromQuery] Guid? doctorId = null) {
            try {
                var queues = await _queueingService.GetTodayQueuesAsync(doctorId);
                return Ok(queues);
            } catch (Exception ex) {
                return HandleException(ex, "获取今日排队列表");
            }
        }

        /// <summary>
        /// 获取当前就诊
        /// </summary>
        [HttpGet("current/{doctorId}")]
        public async Task<ActionResult<QueueingDto>> GetCurrentQueue(Guid doctorId) {
            try {
                var current = await _queueingService.GetCurrentQueueAsync(doctorId);
                if (current == null) {
                    return Ok(new { message = "当前无就诊患者" });
                }
                return Ok(current);
            } catch (Exception ex) {
                return HandleException(ex, "获取当前就诊", new { DoctorId = doctorId });
            }
        }

        /// <summary>
        /// 获取下一位等待患者
        /// </summary>
        [HttpGet("next/{doctorId}")]
        public async Task<ActionResult<QueueingDto>> GetNextWaiting(Guid doctorId) {
            try {
                var next = await _queueingService.GetNextWaitingQueueAsync(doctorId);
                if (next == null) {
                    return Ok(new { message = "暂无等待患者" });
                }
                return Ok(next);
            } catch (Exception ex) {
                return HandleException(ex, "获取下一位患者", new { DoctorId = doctorId });
            }
        }

        /// <summary>
        /// 叫号（呼叫下一位）
        /// </summary>
        [HttpPost("call-next/{doctorId}")]
        [Authorize]
        public async Task<ActionResult> CallNext(Guid doctorId) {
            try {
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CallNextAsync(doctorId, operatorId, operatorName);
                
                if (!result) {
                    return Ok(new { message = "暂无等待患者" });
                }
                
                // 获取新的当前患者
                var current = await _queueingService.GetCurrentQueueAsync(doctorId);
                LogOperation("叫号成功", current, current?.Id);
                return Ok(current);
            } catch (Exception ex) {
                return HandleException(ex, "叫号", new { DoctorId = doctorId });
            }
        }

        /// <summary>
        /// 重新排队（过号重排）
        /// </summary>
        [HttpPost("requeue/{id}")]
        public async Task<ActionResult> Requeue(Guid id) {
            try {
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.RequeueAsync(id, operatorId, operatorName);
                
                if (!result) {
                    return BadRequest(new ProblemDetails { 
                        Title = "操作失败", 
                        Detail = "该排队记录不能重新排队", 
                        Status = 400 
                    });
                }
                
                LogOperation("重新排队成功", null, id);
                return Ok(new { message = "重新排队成功" });
            } catch (Exception ex) {
                return HandleException(ex, "重新排队", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 标记为过号
        /// </summary>
        [HttpPost("miss/{id}")]
        public async Task<ActionResult> MarkAsMissed(Guid id) {
            try {
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.MarkAsMissedAsync(id, operatorId, operatorName);
                
                if (!result) {
                    return NotFound(new ProblemDetails { 
                        Title = "资源未找到", 
                        Detail = "排队记录不存在", 
                        Status = 404 
                    });
                }
                
                LogOperation("标记过号成功", null, id);
                return Ok(new { message = "已标记为过号" });
            } catch (Exception ex) {
                return HandleException(ex, "标记过号", new { QueueingId = id });
            }
        }

        /// <summary>
        /// 获取排队统计
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<QueueStatisticsDto>> GetStatistics([FromQuery] Guid? doctorId = null) {
            try {
                var statistics = await _queueingService.GetStatisticsAsync(doctorId);
                return Ok(statistics);
            } catch (Exception ex) {
                return HandleException(ex, "获取排队统计");
            }
        }

        /// <summary>
        /// 插队（VIP或加急）
        /// </summary>
        [HttpPost("insert/{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult> InsertQueue(Guid id, [FromBody] int position) {
            try {
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.InsertQueueAsync(id, position, operatorId, operatorName);
                
                if (!result) {
                    return BadRequest(new ProblemDetails { 
                        Title = "操作失败", 
                        Detail = "插队操作失败", 
                        Status = 400 
                    });
                }
                
                LogOperation("插队成功", null, id);
                return Ok(new { message = "插队成功" });
            } catch (Exception ex) {
                return HandleException(ex, "插队", new { QueueingId = id, Position = position });
            }
        }

        #endregion
    }
}