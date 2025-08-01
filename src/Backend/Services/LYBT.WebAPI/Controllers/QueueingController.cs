using Asp.Versioning;
using LYBT.Shared.Models.Enums;
using LYBT.Common.Models;
using LYBT.Shared.Models.Common;
using LYBT.Models.Queueing;
using LYBT.Module.Queueing.Interfaces;
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
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 获取排队列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<QueueingDto>>>> GetList() {
            try {
                var list = await _queueingService.GetListAsync();
                return Ok(ApiResponse<List<QueueingDto>>.Success(list));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取排队列表失败");
                return StatusCode(500, ApiResponse<List<QueueingDto>>.Fail("获取排队列表失败", 500));
            }
        }

        /// <summary>
        /// 获取排队详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<QueueingDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<QueueingDetailDto>.Fail("排队ID不能为空", 400));
                }

                var detail = await _queueingService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(ApiResponse<QueueingDetailDto>.Fail("排队记录不存在", 404));
                }
                return Ok(ApiResponse<QueueingDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取排队详情失败，ID: {QueueingId}", id);
                return StatusCode(500, ApiResponse<QueueingDetailDto>.Fail("获取排队详情失败", 500));
            }
        }

        /// <summary>
        /// 新增排队
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] QueueingCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.AddAsync(dto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("新增排队失败", 400));
                }

                _logger.LogInformation("新增排队成功，操作者: {OperatorName}({OperatorId})", operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "新增排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增排队失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增排队失败", 500));
            }
        }

        /// <summary>
        /// 编辑排队
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] QueueingEditDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(ApiResponse<object>.Fail("编辑排队失败", 400));
                }

                _logger.LogInformation("编辑排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", dto.Id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "编辑排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑排队失败，排队ID: {QueueingId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑排队失败", 500));
            }
        }

        /// <summary>
        /// 删除排队
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("排队ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.DeleteAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("排队记录不存在", 404));
                }

                _logger.LogInformation("删除排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "删除排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除排队失败", 500));
            }
        }

        /// <summary>
        /// 取消排队
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("排队ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CancelAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("排队记录不存在", 404));
                }

                _logger.LogInformation("取消排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "取消排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "取消排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("取消排队失败", 500));
            }
        }

        /// <summary>
        /// 完成排队
        /// </summary>
        [HttpPost("complete/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Complete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("排队ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.CompleteAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("排队记录不存在", 404));
                }

                _logger.LogInformation("完成排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "完成排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "完成排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("完成排队失败", 500));
            }
        }

        /// <summary>
        /// 暂停排队
        /// </summary>
        [HttpPost("hold/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Hold(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("排队ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _queueingService.HoldAsync(id);
                if (!result) {
                    return NotFound(ApiResponse<object>.Fail("排队记录不存在", 404));
                }

                _logger.LogInformation("暂停排队成功，排队ID: {QueueingId}，操作者: {OperatorName}({OperatorId})", id, operatorName, operatorId);
                return Ok(ApiResponse<object>.Success(new { }, "暂停排队成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "暂停排队失败，排队ID: {QueueingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("暂停排队失败", 500));
            }
        }
    }
}