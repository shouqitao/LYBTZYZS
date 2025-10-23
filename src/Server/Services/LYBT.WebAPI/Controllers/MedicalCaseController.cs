using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
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
        /// 获取待看诊医案列表（Status=Active）
        /// Epic #1583 - Phase 5
        /// </summary>
        [HttpGet("pending")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult<ApiResponse<List<PendingMedicalCaseDto>>>> GetPendingCases()
        {
            try
            {
                var result = await _medicalCaseService.GetPendingCasesAsync();
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PendingMedicalCaseDto>>(ex, "获取待看诊列表", null);
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

        #region Issue #1477: 子实体更新API（架构纠正v2）

        /// <summary>
        /// 更新病案的诊断信息 (Issue #1477 架构纠正v2)
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        /// <remarks>
        /// 架构说明：
        /// - MedicalCase是聚合根，所有写入操作必须通过它进行
        /// - Consultation作为子实体，通过此API更新（而非ConsultationController）
        /// - 1:1:1关系：MedicalCase.Id == Consultation.Id == Prescription.Id
        /// </remarks>
        [HttpPut("{id}/consultation")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(
            Guid id,
            [FromBody] ConsultationUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<ConsultationDto>(id, "病案ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<ConsultationDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                var result = await _medicalCaseService.UpdateConsultationAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新病案诊断信息", result.Data, id);
                }

                return HandleServiceResult(result, "诊断信息更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "更新病案诊断信息", new { id, dto });
            }
        }

        /// <summary>
        /// 更新病案的处方信息 (Issue #1477 架构纠正v2)
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <param name="dto">处方更新信息</param>
        /// <returns>更新后的处方信息</returns>
        /// <remarks>
        /// 架构说明：
        /// - MedicalCase是聚合根，所有写入操作必须通过它进行
        /// - Prescription作为子实体，通过此API更新（而非PrescriptionsController）
        /// - 1:1:1关系：MedicalCase.Id == Consultation.Id == Prescription.Id
        /// </remarks>
        [HttpPut("{id}/prescription")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> UpdatePrescription(
            Guid id,
            [FromBody] PrescriptionUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<PrescriptionDto>(id, "病案ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<PrescriptionDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                var result = await _medicalCaseService.UpdatePrescriptionAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新病案处方信息", result.Data, id);
                }

                return HandleServiceResult(result, "处方信息更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "更新病案处方信息", new { id, dto });
            }
        }

        #endregion

        /// <summary>
        /// 批量删除医疗案例（软删除）(Issue #1169)
        /// </summary>
        /// <param name="request">批量删除请求</param>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<BatchOperationResultDto>>> BatchDeleteMedicalCases([FromBody] BatchDeleteRequestDto request)
        {
            try
            {
                // 验证请求
                if (request.Ids == null || request.Ids.Count == 0)
                {
                    return ValidationFail<BatchOperationResultDto>("ID列表不能为空");
                }

                if (request.Ids.Count > 100)
                {
                    return ValidationFail<BatchOperationResultDto>("批量操作最多支持100条记录");
                }

                var result = await _medicalCaseService.BatchDeleteAsync(request.Ids);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("批量删除医疗案例", 
                        new { TotalCount = result.Data.TotalCount, SuccessCount = result.Data.SuccessCount }, 
                        null);
                }

                return HandleServiceResult(result, result.Data?.Message ?? "批量删除完成");
            }
            catch (Exception ex)
            {
                return HandleException<BatchOperationResultDto>(ex, "批量删除医疗案例", new { IdCount = request.Ids?.Count });
            }
        }
    }
}
