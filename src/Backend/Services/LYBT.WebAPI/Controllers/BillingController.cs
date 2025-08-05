using Asp.Versioning;
using LYBT.Module.Billing.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Billing;
using LYBT.Shared.Models.Enums;
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
    public class BillingController : BaseController {
        private readonly IBillingService _billingService;

        /// <summary>
        /// 构造方法，注入业务服务
        /// </summary>
        public BillingController(IBillingService billingService, IMemoryCache cache, ILogger<BillingController> logger) 
            : base(logger, cache) {
            _billingService = billingService;
        }

        /// <summary>
        /// 获取费用结算列表 (RESTful GET /Billing) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<BillingDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? billingId = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] BillingStatus? status = null,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null) {
            try {
                // 如果没有任何查询条件且请求第一页，使用缓存的完整列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(billingId) && 
                    string.IsNullOrEmpty(patientName) && string.IsNullOrEmpty(doctorName) && !status.HasValue &&
                    string.IsNullOrEmpty(paymentMethod) && !startDate.HasValue && !endDate.HasValue &&
                    !minAmount.HasValue && !maxAmount.HasValue) {
                    
                    const string cacheKey = "billing_list";
                    if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                        list = await _billingService.GetListAsync();
                        _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                    }
                    
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<BillingDto>();
                    var result = new PaginatedResult<BillingDto> {
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
                var pagedResult = await _billingService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取费用结算列表");
            }
        }

        /// <summary>
        /// 分页获取费用结算列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<BillingDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _billingService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取费用结算列表");
            }
        }

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BillingDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var cacheKey = $"billing_detail_{id}";
                if (!_cache.TryGetValue(cacheKey, out BillingDetailDto? detail)) {
                    detail = await _billingService.GetByIdAsync(id);
                    if (detail != null) {
                        _cache.Set(cacheKey, detail, TimeSpan.FromMinutes(10));
                    }
                }

                if (detail == null)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取费用结算详情", new { BillingId = id });
            }
        }

        /// <summary>
        /// 新增费用结算
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BillingDto>> Add([FromBody] BillingCreateDto billingCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _billingService.AddAsync(billingCreateDto);
                if (result == null)
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增费用结算失败" });

                // 清除缓存
                _cache.Remove("billing_list");
                
                LogOperation("新增费用结算成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增费用结算");
            }
        }

        /// <summary>
        /// 编辑费用结算
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update([FromBody] BillingEditDto billingEditDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _billingService.UpdateAsync(billingEditDto);
                if (!result)
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑费用结算失败" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{billingEditDto.Id}");

                return Ok("编辑费用结算成功");
            } catch (Exception ex) {
                return HandleException(ex, "编辑费用结算", new { BillingId = billingEditDto.Id });
            }
        }

        /// <summary>
        /// 删除费用结算
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _billingService.DeleteAsync(id);
                if (!result)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("删除费用结算成功");
            } catch (Exception ex) {
                return HandleException(ex, "删除费用结算", new { BillingId = id });
            }
        }

        [HttpPatch("{id}/paid")]
        public async Task<ActionResult<object>> MarkAsPaid(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.MarkAsPaidAsync(id);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("标记为已付款成功");
            } catch (Exception ex) {
                return HandleException(ex, "标记为已付款", new { BillingId = id });
            }
        }

        [HttpPatch("{id}/completed")]
        public async Task<ActionResult<object>> MarkAsCompleted(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.MarkAsCompletedAsync(id);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("标记为已完成成功");
            } catch (Exception ex) {
                return HandleException(ex, "标记为已完成", new { BillingId = id });
            }
        }

        [HttpPost("request-refund/{id}")]
        public async Task<ActionResult<object>> RequestRefund(Guid id, [FromBody] string reason) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.RequestRefundAsync(id, reason);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("申请退款成功");
            } catch (Exception ex) {
                return HandleException(ex, "申请退款", new { BillingId = id });
            }
        }

        [HttpPost("approve-refund/{id}")]
        public async Task<ActionResult<object>> ApproveRefund(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.ApproveRefundAsync(id);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("批准退款成功");
            } catch (Exception ex) {
                return HandleException(ex, "批准退款", new { BillingId = id });
            }
        }

        [HttpPost("reject-refund/{id}")]
        public async Task<ActionResult<object>> RejectRefund(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.RejectRefundAsync(id);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("拒绝退款成功");
            } catch (Exception ex) {
                return HandleException(ex, "拒绝退款", new { BillingId = id });
            }
        }

        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<object>> Cancel(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "费用结算ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var success = await _billingService.CancelAsync(id);
                if (!success)
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "费用结算不存在" });

                // 清除缓存
                _cache.Remove("billing_list");
                _cache.Remove($"billing_detail_{id}");

                return Ok("取消费用结算成功");
            } catch (Exception ex) {
                return HandleException(ex, "取消费用结算", new { BillingId = id });
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<BillingDto>>> GetByPatientId(Guid patientId) {
            try {
                var validationResult = ValidateGuid(patientId, "患者ID");
                if (validationResult != null) return validationResult;

                var cacheKey = $"billing_patient_{patientId}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetByPatientIdAsync(patientId);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(list ?? new List<BillingDto>());
            } catch (Exception ex) {
                return HandleException(ex, "根据患者ID获取费用结算", new { PatientId = patientId });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<BillingDto>>> Search([FromQuery] string keyword = "") {
            try {
                var cacheKey = $"billing_search_{keyword}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.SearchAsync(keyword);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(5));
                }
                return Ok(list ?? new List<BillingDto>());
            } catch (Exception ex) {
                return HandleException(ex, "搜索费用结算", new { Keyword = keyword });
            }
        }

        [HttpGet("refundable")]
        public async Task<ActionResult<List<BillingDto>>> GetRefundableBills() {
            try {
                const string cacheKey = "billing_refundable";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetRefundableBillsAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(list ?? new List<BillingDto>());
            } catch (Exception ex) {
                return HandleException(ex, "获取可退款费用结算");
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<BillingDto>>> GetByStatus(BillingStatus status) {
            try {
                var cacheKey = $"billing_status_{status}";
                if (!_cache.TryGetValue(cacheKey, out List<BillingDto>? list)) {
                    list = await _billingService.GetByStatusAsync(status);
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(list ?? new List<BillingDto>());
            } catch (Exception ex) {
                return HandleException(ex, "根据状态获取费用结算", new { Status = status });
            }
        }
    }
}