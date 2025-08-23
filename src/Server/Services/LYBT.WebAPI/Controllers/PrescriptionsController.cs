using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
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
        public async Task<ActionResult<ApiResponse<PagedData<PrescriptionDto>>>> GetList(
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

                    var result = await _service.SearchAsync("");
                    if (!result.IsSuccess || result.Data == null)
                    {
                        return BusinessFailPaged<PrescriptionDto>(result.ErrorMessage ?? "查询失败", ApiErrorCodes.INTERNAL_ERROR);
                    }
                    
                    var list = result.Data;
                    var totalCount = list.Count;
                    var pagedList = list.Take(pageSize).ToList();
                    var paginatedResult = new PaginatedResult<PrescriptionDto>
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
                if (validationResult != null) return validationResult;

                var result = await _service.GetByIdAsync(id);
                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PrescriptionDto>(result.ErrorMessage ?? "处方不存在", ApiErrorCodes.PRESCRIPTION_NOT_FOUND);
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
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.CreateAsync(dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "新增处方失败", ApiErrorCodes.DATA_SAVE_FAILED);
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
                if (idValidation != null) return idValidation;

                var modelValidation = ValidateModel<PrescriptionDto>();
                if (modelValidation != null) return modelValidation;

                // 确保DTO的ID与路由参数一致
                dto.Id = id;
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "编辑处方失败", ApiErrorCodes.DATA_UPDATE_FAILED);
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
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess || !result.Data)
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
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Cancel(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                               
                // 临时返回未实现错误
                return BusinessFail<PrescriptionDto>("作废功能暂未实现", ApiErrorCodes.INTERNAL_ERROR);
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "作废处方", id);
            }
        }
    }
}