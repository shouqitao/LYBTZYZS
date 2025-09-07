using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<ActionResult<ApiResponse<PagedResult<PrescriptionDto>>>> GetList(
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
                    return ValidationFailPaged<PrescriptionDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) &&
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(diagnosis) && !status.HasValue &&
                    !startDate.HasValue && !endDate.HasValue && !minDosageCount.HasValue && !maxDosageCount.HasValue)
                {
                    var result = await _service.SearchAsync(string.Empty);
                    if (!result.IsSuccess || result.Data == null)
                    {
                        return BusinessFailPaged<PrescriptionDto>(result.ErrorMessage ?? "查询失败", ApiErrorCodes.INTERNALERROR);
                    }

                    var list = result.Data;
                    var totalCount = list.Count;
                    var pagedList = list.Take(pageSize).ToList();
                    var paginatedResult = new PagedResult<PrescriptionDto>
                    {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Success(paginatedResult, "查询成功");
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new PrescriptionQueryDto
                {
                    PageIndex = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    PrescriptionStatus = status,
                    // 注意：其他属性暂时移除，因为PrescriptionQueryDto中没有定义
                    // PatientName、DoctorName、Diagnosis、MinDosageCount、MaxDosageCount等
                };

                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _service.GetPagedAsync(query);
                return HandlePagedServiceResult(pagedResult, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PrescriptionDto>(ex, "获取处方列表", new { page, pageSize, keyword });
            }
        }

        // 移除重复的分页查询接口，统一使用RESTful GET接口

        /// <summary>
        /// 获取处方详情 - 统一API响应格式
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PrescriptionDto>(result.ErrorMessage ?? "处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                // 如果需要转换为PrescriptionDetailDto，这里应该进行映射
                // 暂时假设PrescriptionDto可以用作PrescriptionDetailDto
                var detail = result.Data;
                return Success(detail, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "获取处方详情", id);
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
                if (validationResult != null)
                {
                    return validationResult;
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "新增处方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("新增处方成功", result.Data, result.Data.Id);
                return Success(result.Data, "处方创建成功");
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
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(Guid id, [FromBody] PrescriptionEditDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<PrescriptionDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "编辑处方失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                // 获取更新后的资源 - 可以直接使用更新结果
                var updated = result.Data;
                LogOperation("编辑处方成功", updated, dto.Id);
                return Success(updated, "处方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "编辑处方", new { id, dto });
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
                if (validationResult != null)
                {
                    return validationResult;
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess || !result.Data)
                {
                    return NotFound("处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
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
        /// 根据患者ID获取处方历史 - 统一API响应格式
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByPatientId(Guid patientId)
        {
            try
            {
                var validation = ValidateGuid<List<PrescriptionDto>>(patientId, "患者ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _service.GetByPatientIdAsync(patientId);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionDto>>(ex, "获取患者处方历史", patientId);
            }
        }

        /// <summary>
        /// 根据医案ID获取处方记录 - 统一API响应格式
        /// </summary>
        [HttpGet("medical-case/{caseId}")]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCaseId(Guid caseId)
        {
            try
            {
                var validation = ValidateGuid<List<PrescriptionDto>>(caseId, "医案ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _service.GetByMedicalCaseIdAsync(caseId);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionDto>>(ex, "获取医案处方记录", caseId);
            }
        }

        /// <summary>
        /// 高级搜索处方 - 统一API响应格式
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> AdvancedSearch([FromBody] PrescriptionSearchDto criteria)
        {
            try
            {
                var validation = ValidateModel<List<PrescriptionDto>>();
                if (validation != null)
                {
                    return validation;
                }

                // 如果提供了基础搜索关键词，使用基础搜索
                if (!string.IsNullOrEmpty(criteria.Keyword))
                {
                    var result = await _service.SearchAsync(criteria.Keyword);
                    return HandleServiceResult(result, $"搜索完成，找到{result.Data?.Count ?? 0}条记录");
                }

                // 否则返回空结果（暂不支持复杂搜索条件）
                var emptyResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
                return HandleServiceResult(emptyResult, "高级搜索功能待完善，请使用基础关键词搜索");
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionDto>>(ex, "高级搜索处方", criteria);
            }
        }

        /// <summary>
        /// 复制处方 - 统一API响应格式
        /// </summary>
        [HttpPost("{id}/copy")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Copy(Guid id, [FromBody] PrescriptionCopyDto dto)
        {
            try
            {
                var validation = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validation != null)
                {
                    return validation;
                }

                if (string.IsNullOrWhiteSpace(dto?.NewName))
                {
                    return ValidationFail<PrescriptionDto>("新处方名称不能为空");
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CopyAsync(id, dto.NewName);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("复制处方", result.Data, result.Data.Id);
                }

                return HandleServiceResult(result, "处方复制成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "复制处方", new { id, dto });
            }
        }

        /// <summary>
        /// 验证处方数据 - 统一API响应格式
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<PrescriptionValidationResult>>> Validate([FromBody] PrescriptionCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<PrescriptionValidationResult>();
                if (validation != null)
                {
                    return validation;
                }

                var result = await _service.ValidateAsync(dto);
                return HandleServiceResult(result, "验证完成");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionValidationResult>(ex, "验证处方数据", dto);
            }
        }
    }
}
