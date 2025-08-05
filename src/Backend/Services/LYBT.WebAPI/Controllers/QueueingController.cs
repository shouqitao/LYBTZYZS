using Asp.Versioning;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 排队管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class QueueingController : ControllerBase {
        private readonly IQueueingService _queueingService;
        private readonly ILogger<QueueingController> _logger;

        /// <summary>
        /// 构造方法，注入排队服务
        /// </summary>
        public QueueingController(IQueueingService queueingService, ILogger<QueueingController> logger) {
            _queueingService = queueingService;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前操作者信息
        /// </summary>
        private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator() {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName)) {
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.RegistrationStaff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
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
                _logger.LogError(ex, "获取排队列表失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "获取排队列表失败", Status = 500 });
            }
        }

        /// <summary>
        /// 分页获取排队列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<QueueingDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = $"参数验证失败：{errors}", Status = 400 });
                }

                var (_, _, operatorRole) = GetOperator();
                var result = await _queueingService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "分页获取排队列表失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "分页获取排队列表失败", Status = 500 });
            }
        }

        /// <summary>
        /// 获取排队详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<QueueingDetailDto>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "排队ID不能为空", Status = 400 });
                }

                var detail = await _queueingService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }
                return Ok(detail);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取排队详情失败，ID: {QueueingId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "获取排队详情失败", Status = 500 });
            }
        }

        /// <summary>
        /// 新增排队
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> Add([FromBody] QueueingCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = $"参数验证失败：{errors}", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.AddAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "新增排队失败", Status = 400 });
                }

                _logger.LogInformation("新增排队成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(new { message = "新增排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "新增排队失败");
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "新增排队失败", Status = 500 });
            }
        }

        /// <summary>
        /// 编辑排队
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] QueueingEditDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = $"参数验证失败：{errors}", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "编辑排队失败", Status = 400 });
                }

                _logger.LogInformation("编辑排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(new { message = "编辑排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑排队失败，排队ID: {QueueingId}", dto.Id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "编辑排队失败", Status = 500 });
            }
        }

        /// <summary>
        /// 删除排队
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "排队ID不能为空", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                _logger.LogInformation("删除排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(new { message = "删除排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "删除排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "删除排队失败", Status = 500 });
            }
        }

        /// <summary>
        /// 取消排队
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<object>> Cancel(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "排队ID不能为空", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CancelAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                _logger.LogInformation("取消排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(new { message = "取消排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "取消排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "取消排队失败", Status = 500 });
            }
        }

        /// <summary>
        /// 完成排队
        /// </summary>
        [HttpPost("complete/{id}")]
        public async Task<ActionResult<object>> Complete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "排队ID不能为空", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CompleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                _logger.LogInformation("完成排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(new { message = "完成排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "完成排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "完成排队失败", Status = 500 });
            }
        }

        /// <summary>
        /// 暂停排队
        /// </summary>
        [HttpPost("hold/{id}")]
        public async Task<ActionResult<object>> Hold(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "排队ID不能为空", Status = 400 });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.HoldAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "排队记录不存在", Status = 404 });
                }

                _logger.LogInformation("暂停排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(new { message = "暂停排队成功" });
            } catch (Exception ex) {
                _logger.LogError(ex, "暂停排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, new ProblemDetails { Title = "服务器内部错误", Detail = "暂停排队失败", Status = 500 });
            }
        }
    }
}