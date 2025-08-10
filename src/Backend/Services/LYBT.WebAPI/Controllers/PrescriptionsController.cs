using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 处方管理 API - 统一API响应格式
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : BaseApiController
    {
        private readonly IPrescriptionService _service;
        public PrescriptionsController(IPrescriptionService service, IMemoryCache cache, ILogger<PrescriptionsController> logger)
            : base(logger, cache)
        {
            _service = service;
        }

        /// <summary>
        /// 获取处方列表 (RESTful GET /Prescriptions) - 支持模糊查询和分页 - 统一API响应格式
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedResult<PrescriptionDto>>>> GetList(
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
            [FromQuery] int? maxDosageCount = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFail<PaginatedResult<PrescriptionDto>>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) &&
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(diagnosis) && !status.HasValue &&
                    !startDate.HasValue && !endDate.HasValue && !minDosageCount.HasValue && !maxDosageCount.HasValue)
                {

                    var list = await _service.GetAllAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<PrescriptionDto>();
                    var result = new PaginatedResult<PrescriptionDto>
                    {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Success<PaginatedResult<PrescriptionDto>>(result, "查询成功");
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new PagedQueryBaseDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword
                };

                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _service.GetPagedAsync(query);
                return Success<PaginatedResult<PrescriptionDto>>(pagedResult, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PaginatedResult<PrescriptionDto>>(ex, "获取处方列表", new { page, pageSize, keyword });
            }
        }

        // 移除重复的分页查询接口，统一使用RESTful GET接口

        /// <summary>
        /// 获取处方详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDetailDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDetailDto>(id, "处方ID");
                if (validationResult != null) return validationResult;

                var detail = await _service.GetByIdAsync(id.ToString());
                if (detail == null)
                {
                    return NotFound<PrescriptionDetailDto>("处方不存在", ApiErrorCodes.PRESCRIPTION_NOT_FOUND);
                }
                return Success(detail, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDetailDto>(ex, "获取处方详情", id);
            }
        }

        /// <summary>
        /// 新增处方 - 统一API响应格式
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add([FromBody] PrescriptionCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<PrescriptionDto>();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CreateAsync(dto, operatorId, operatorName);
                if (result == null)
                {
                    return BusinessFail<PrescriptionDto>("新增处方失败", ApiErrorCodes.DATA_SAVE_FAILED);
                }

                LogOperation("新增处方成功", result, result.Id);
                return Success(result, "处方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "新增处方", dto);
            }
        }

        /// <summary>
        /// 编辑处方 - 统一API响应格式
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDetailDto>>> Update(Guid id, [FromBody] PrescriptionEditDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PrescriptionDetailDto>(id, "处方ID");
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<PrescriptionDetailDto>();
                if (modelValidation != null) return modelValidation;

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(dto, operatorId, operatorName);
                if (!result)
                {
                    return BusinessFail<PrescriptionDetailDto>("编辑处方失败", ApiErrorCodes.DATA_UPDATE_FAILED);
                }

                // 获取更新后的资源
                var updated = await _service.GetByIdAsync(dto.Id.ToString());
                LogOperation("编辑处方成功", updated, dto.Id);
                return Success(updated, "处方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDetailDto>(ex, "编辑处方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除处方 - 统一API响应格式
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id.ToString(), operatorId, operatorName);
                if (!result)
                {
                    return NotFound("处方不存在", ApiErrorCodes.PRESCRIPTION_NOT_FOUND);
                }

                LogOperation("删除处方成功", null, id);
                return Success("删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除处方", id);
            }
        }

        /// <summary>
        /// 作废处方 - 统一API响应格式
        /// </summary>
        [HttpPost("void/{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDetailDto>>> Cancel(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDetailDto>(id, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CancelAsync(id.ToString(), operatorId, operatorName);
                if (!result)
                {
                    return NotFound<PrescriptionDetailDto>("处方不存在", ApiErrorCodes.PRESCRIPTION_NOT_FOUND);
                }

                // 获取更新后的资源
                var updated = await _service.GetByIdAsync(id.ToString());
                LogOperation("作废处方成功", updated, id);
                return Success(updated, "处方已作废");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDetailDto>(ex, "作废处方", id);
            }
        }
    }
}