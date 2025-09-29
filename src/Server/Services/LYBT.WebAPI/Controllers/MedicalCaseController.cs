using Asp.Versioning;
using LYBT.Core.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize]
    public class MedicalCaseController : BaseApiController
    {
        private readonly IMedicalCaseService _medicalCaseService;

        public MedicalCaseController(
            IMedicalCaseService medicalCaseService,
            ILogger<MedicalCaseController> logger,
            IMemoryCache cache) : base(logger, cache)
        {
            _medicalCaseService = medicalCaseService;
        }

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 1200, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "MedicalCaseCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseDto>>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<MedicalCaseDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _medicalCaseService.GetPagedAsync(page, pageSize, keyword);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<MedicalCaseDto>(ex, "获取医疗案例列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<MedicalCaseDto>(id, "医疗案例ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.GetByIdAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "获取医疗案例详情", id);
            }
        }

        /// <summary>
        /// 根据ID获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        [HttpGet("{id}/with-details")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetByIdWithDetails(Guid id)
        {
            try
            {
                var validation = ValidateGuid<MedicalCaseDetailDto>(id, "医疗案例ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.GetByIdWithDetailsAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDetailDto>(ex, "获取完整医疗案例", id);
            }
        }

        /// <summary>
        /// 创建新的医疗案例
        /// </summary>
        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// 作为聚合根统一管理整个诊疗流程
        /// </summary>
        [HttpPost("with-details")]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> CreateWithDetails([FromBody] MedicalCaseWithDetailsCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<MedicalCaseDto>();
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.CreateWithDetailsAsync(
                    dto.MedicalCase,
                    dto.Consultation,
                    dto.Prescription);
                    
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建完整医疗案例", result.Data, result.Data.Id);
                }

                return HandleServiceResult(result, "医疗案例创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "创建完整医疗案例", dto);
            }
        }

        /// <summary>
        /// 创建医疗案例（基础信息）
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> Create([FromBody] MedicalCaseCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<MedicalCaseDto>();
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建医疗案例", result.Data, result.Data.Id);
                }

                return HandleServiceResult(result, "医疗案例创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "创建医疗案例", dto);
            }
        }

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> Update(Guid id, [FromBody] MedicalCaseUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<MedicalCaseDto>(id, "医疗案例ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<MedicalCaseDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                var result = await _medicalCaseService.UpdateAsync(id, dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新医疗案例", result.Data, id);
                }

                return HandleServiceResult(result, "医疗案例更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "更新医疗案例", new { id, dto });
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "医疗案例ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.DeleteAsync(id);
                return HandleServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除医疗案例", id);
            }
        }
    }
}