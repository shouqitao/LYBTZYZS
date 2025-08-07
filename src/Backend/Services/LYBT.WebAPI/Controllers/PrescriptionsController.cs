using Asp.Versioning;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 处方管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : BaseController {
        private readonly IPrescriptionService _service;
        public PrescriptionsController(IPrescriptionService service, IMemoryCache cache, ILogger<PrescriptionsController> logger) 
            : base(logger, cache) {
            _service = service;
        }

        /// <summary>
        /// 获取处方列表 (RESTful GET /Prescriptions) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<PrescriptionDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] string? diagnosis = null,
            [FromQuery] PrescriptionStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? minDosageCount = null,
            [FromQuery] int? maxDosageCount = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(diagnosis) && !status.HasValue &&
                    !startDate.HasValue && !endDate.HasValue && !minDosageCount.HasValue && !maxDosageCount.HasValue) {
                    
                    var list = await _service.GetAllAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<PrescriptionDto>();
                    var result = new PaginatedResult<PrescriptionDto> {
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
                var pagedResult = await _service.GetPagedAsync(query);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取处方列表");
            }
        }

        // 移除重复的分页查询接口，统一使用RESTful GET接口

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PrescriptionDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null) return validationResult;

                var detail = await _service.GetByIdAsync(id.ToString());
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "处方不存在", Status = 404 });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取处方详情", new { PrescriptionId = id });
            }
        }

        /// <summary>
        /// 新增处方
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PrescriptionDto>> Add([FromBody] PrescriptionCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CreateAsync(dto, operatorId, operatorName);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "新增处方失败", Status = 400 });
                }

                LogOperation("新增处方成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增处方");
            }
        }

        /// <summary>
        /// 编辑处方
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PrescriptionDto>> Update(Guid id, [FromBody] PrescriptionEditDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(dto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "编辑处方失败", Status = 400 });
                }

                // 获取更新后的资源
                var updated = await _service.GetByIdAsync(dto.Id.ToString());
                LogOperation("编辑处方成功", updated, dto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑处方", new { PrescriptionId = dto.Id });
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id.ToString(), operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "处方不存在", Status = 404 });
                }

                LogOperation("删除处方成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除处方", new { PrescriptionId = id });
            }
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        [HttpPost("void/{id}")]
        public async Task<ActionResult<PrescriptionDto>> Cancel(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CancelAsync(id.ToString(), operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "处方不存在", Status = 404 });
                }

                // 获取更新后的资源
                var updated = await _service.GetByIdAsync(id.ToString());
                LogOperation("作废处方成功", updated, id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "作废处方", new { PrescriptionId = id });
            }
        }
    }
}