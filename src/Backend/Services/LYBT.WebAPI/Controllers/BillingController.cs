using Asp.Versioning;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Common.Models;
using LYBT.Models.Billing;
using LYBT.Module.Billing.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 费用结算 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase {
        private readonly IBillingService _billingService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BillingController> _logger;

        /// <summary>
        /// 构造方法，注入业务服务
        /// </summary>
        public BillingController(IBillingService billingService, IMemoryCache cache, ILogger<BillingController> logger) {
            _billingService = billingService;
            _cache = cache;
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
        /// 获取费用结算列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<BillingDto>>>> GetList() {
            try {
                const string cacheKey = "billing_list";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetListAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(ApiResponse<List<BillingDto>>.Success(list ?? new List<BillingDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取费用结算列表失败");
                return StatusCode(500, ApiResponse<List<BillingDto>>.Fail("获取费用结算列表失败", 500));
            }
        }

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BillingDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<BillingDetailDto>.Fail("费用结算ID不能为空", 400));
                }

                var cacheKey = $"billing_detail_{id}";
                if (!_cache.TryGetValue(cacheKey, out BillingDetailDto? detail)) {
                    detail = await _billingService.GetByIdAsync(id);
                    if (detail != null) {
                        _cache.Set(cacheKey, detail, TimeSpan.FromMinutes(10));
                    }
                }

                if (detail == null)
                    return NotFound(ApiResponse<BillingDetailDto>.Fail("费用结算不存在", 404));
                return Ok(ApiResponse<BillingDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取费用结算详情失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<BillingDetailDto>.Fail("获取费用结算详情失败", 500));
            }
        }

        /// <summary>
        /// 新增费用结算
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] BillingCreateDto billingCreateDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _billingService.AddAsync(billingCreateDto);
                if (!result)
                    return BadRequest(ApiResponse<object>.Fail("新增费用结算失败", 400));

                // 清除缓存
                _cache.Remove("billing_list");

                return Ok(ApiResponse<object>.Success("新增费用结算成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增费用结算失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增费用结算失败", 500));
            }
        }

        /// <summary>
        /// 编辑费用结算
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] BillingEditDto billingEditDto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _billingService.UpdateAsync(billingEditDto);
                if (!result)
                    return BadRequest(ApiResponse<object>.Fail("编辑费用结算失败", 400));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{billingEditDto.Id}");

                return Ok(ApiResponse<object>.Success("编辑费用结算成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑费用结算失败，ID: {BillingId}", billingEditDto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑费用结算失败", 500));
            }
        }

        /// <summary>
        /// 删除费用结算
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _billingService.DeleteAsync(id);
                if (!result)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("删除费用结算成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除费用结算失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除费用结算失败", 500));
            }
        }

        [HttpPost("mark-paid/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAsPaid(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.MarkAsPaidAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("标记为已付款成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "标记为已付款失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("标记为已付款失败", 500));
            }
        }

        [HttpPost("complete/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> MarkAsCompleted(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.MarkAsCompletedAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("标记为已完成成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "标记为已完成失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("标记为已完成失败", 500));
            }
        }

        [HttpPost("request-refund/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RequestRefund(Guid id, [FromBody] string reason) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.RequestRefundAsync(id, reason);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("申请退款成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "申请退款失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("申请退款失败", 500));
            }
        }

        [HttpPost("approve-refund/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> ApproveRefund(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.ApproveRefundAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("批准退款成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "批准退款失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("批准退款失败", 500));
            }
        }

        [HttpPost("reject-refund/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> RejectRefund(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.RejectRefundAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("拒绝退款成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "拒绝退款失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("拒绝退款失败", 500));
            }
        }

        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("费用结算ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.CancelAsync(id);
                if (!success)
                    return NotFound(ApiResponse<object>.Fail("费用结算不存在", 404));

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok(ApiResponse<object>.Success("取消费用结算成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "取消费用结算失败，ID: {BillingId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("取消费用结算失败", 500));
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<ApiResponse<List<BillingDto>>>> GetByPatientId(Guid patientId) {
            try {
                if (patientId == Guid.Empty) {
                    return BadRequest(ApiResponse<List<BillingDto>>.Fail("患者ID不能为空", 400));
                }

                var cacheKey = $"billing_patient_{patientId}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetByPatientIdAsync(patientId);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(ApiResponse<List<BillingDto>>.Success(list ?? new List<BillingDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "根据患者ID获取费用结算失败，患者ID: {PatientId}", patientId);
                return StatusCode(500, ApiResponse<List<BillingDto>>.Fail("根据患者ID获取费用结算失败", 500));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<BillingDto>>>> Search([FromQuery] string keyword = "") {
            try {
                var cacheKey = $"billing_search_{keyword}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.SearchAsync(keyword);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(5));
                }
                return Ok(ApiResponse<List<BillingDto>>.Success(list ?? new List<BillingDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "搜索费用结算失败，关键词: {Keyword}", keyword);
                return StatusCode(500, ApiResponse<List<BillingDto>>.Fail("搜索费用结算失败", 500));
            }
        }

        [HttpGet("refundable")]
        public async Task<ActionResult<ApiResponse<List<BillingDto>>>> GetRefundableBills() {
            try {
                const string cacheKey = "billing_refundable";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetRefundableBillsAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(ApiResponse<List<BillingDto>>.Success(list ?? new List<BillingDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取可退款费用结算失败");
                return StatusCode(500, ApiResponse<List<BillingDto>>.Fail("获取可退款费用结算失败", 500));
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<ApiResponse<List<BillingDto>>>> GetByStatus(BillingStatus status) {
            try {
                var cacheKey = $"billing_status_{status}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetByStatusAsync(status);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(ApiResponse<List<BillingDto>>.Success(list ?? new List<BillingDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "根据状态获取费用结算失败，状态: {Status}", status);
                return StatusCode(500, ApiResponse<List<BillingDto>>.Fail("根据状态获取费用结算失败", 500));
            }
        }
    }
}