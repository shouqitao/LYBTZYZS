using Asp.Versioning;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药房 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PharmacyController : BaseController {
        private readonly IPharmacyService _pharmacyService;
        /// <summary>
        /// 构造方法，注入药房服务
        /// </summary>
        public PharmacyController(IPharmacyService pharmacyService, IMemoryCache cache, ILogger<PharmacyController> logger) 
            : base(logger, cache) {
            _pharmacyService = pharmacyService;
        }

        /// <summary>
        /// 获取待抓药的处方列表
        /// </summary>
        [HttpGet("waiting")]
        public async Task<ActionResult<List<PharmacyDto>>> GetWaitingList() {
            try {
                var list = await _pharmacyService.GetWaitingListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取待抓药处方列表");
            }
        }

        /// <summary>
        /// 获取药房单列表 (RESTful GET /Pharmacy) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<PharmacyDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] PharmacyStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? needDecoction = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && !status.HasValue && !startDate.HasValue && 
                    !endDate.HasValue && !needDecoction.HasValue) {
                    
                    var list = await _pharmacyService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<PharmacyDto>();
                    var result = new PaginatedResult<PharmacyDto> {
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
                var pagedResult = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取药房单列表");
            }
        }

        /// <summary>
        /// 分页获取药房单列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<PharmacyDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取药房单列表");
            }
        }

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var detail = await _pharmacyService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取药房单详情", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PharmacyDto>> Add([FromBody] PharmacyCreateDto pharmacyCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.AddAsync(pharmacyCreateDto);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增药房单失败" });
                }

                LogOperation("新增药房单成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增药房单");
            }
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update([FromBody] PharmacyEditDto pharmacyEditDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.UpdateAsync(pharmacyEditDto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑药房单失败" });
                }

                // 获取更新后的资源
                var updated = await _pharmacyService.GetByIdAsync(pharmacyEditDto.Id);
                LogOperation("编辑药房单成功", updated, pharmacyEditDto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑药房单", new { PharmacyId = pharmacyEditDto.Id });
            }
        }

        /// <summary>
        /// 删除药房单
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }

                LogOperation("删除药房单成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除药房单", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 标记处方为已抓药
        /// </summary>
        [HttpPost("{id}/prepared")]
        public async Task<ActionResult<object>> MarkAsPrepared(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.MarkAsPreparedAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }

                LogOperation("标记处方为已抓药成功", null, id);
                return Ok(new { message = "标记为已抓药成功" });
            } catch (Exception ex) {
                return HandleException(ex, "标记处方为已抓药", new { PharmacyId = id });
            }
        }
    }
}