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
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        [HttpGet("query")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> Query(
            [FromQuery] string? patientName = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? diagnosisKeyword = null)
        {
            try
            {
                // 至少需要一个查询条件
                if (string.IsNullOrWhiteSpace(patientName) &&
                    !startDate.HasValue &&
                    !endDate.HasValue &&
                    string.IsNullOrWhiteSpace(diagnosisKeyword))
                {
                    return ValidationFail<List<MedicalCaseDto>>("请至少提供一个查询条件");
                }

                // 日期范围验证
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return ValidationFail<List<MedicalCaseDto>>("开始日期不能晚于结束日期");
                }

                var result = await _medicalCaseService.QueryAsync(patientName, startDate, endDate, diagnosisKeyword);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<MedicalCaseDto>>(ex, "查询病案列表", new { patientName, startDate, endDate, diagnosisKeyword });
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
        /// 根据患者ID获取医疗案例列表
        /// Issue #1584 - Bug修复
        /// </summary>
        [HttpGet("by-patient/{patientId}")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "patientId" })]
        public async Task<ActionResult<ApiResponse<List<MedicalCaseDto>>>> GetByPatientId(Guid patientId)
        {
            try
            {
                var validation = ValidateGuid<List<MedicalCaseDto>>(patientId, "患者ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _medicalCaseService.GetByPatientIdAsync(patientId);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<MedicalCaseDto>>(ex, "根据患者ID获取医疗案例列表", patientId);
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

        /// <summary>
        /// 创建病案处方（Issue #1608补充）
        /// </summary>
        [HttpPost("{id}/prescription")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription(
            Guid id,
            [FromBody] PrescriptionCreateDto dto)
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

                var result = await _medicalCaseService.CreatePrescriptionAsync(id, dto);

                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建病案处方", result.Data, id);
                }

                return HandleServiceResult(result, "处方创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "创建病案处方", new { id, dto });
            }
        }

        /// <summary>
        /// 删除病案处方（Issue #1608补充）
        /// </summary>
        [HttpDelete("{id}/prescription")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> DeletePrescription(Guid id)
        {
            try
            {
                var idValidation = ValidateGuid(id, "病案ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var result = await _medicalCaseService.DeletePrescriptionAsync(id);

                if (result.IsSuccess)
                {
                    LogOperation("删除病案处方", null, id);
                }

                return HandleServiceResult(result, "处方删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除病案处方", new { id });
            }
        }


        // ========== Epic #1589 - 三步工作流辅助端点（Issue #1600 Phase 4）==========

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Epic #1589 Phase 1 - 架构合规版本
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">Step1请求参数</param>
        /// <returns>Step1完成状态</returns>
        [HttpPost("{id}/complete-step1")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationStepDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<ConsultationStepDto>>> CompleteStep1(
            Guid id,
            [FromBody] CompleteStep1Request request)
        {
            try
            {
                var validationResult = ValidateGuid<ConsultationStepDto>(id, "医案ID");
                if (validationResult != null) return validationResult;

                var result = await _medicalCaseService.CompleteStep1Async(id, request);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationStepDto>(ex, "完成Step1", new { MedicalCaseId = id });
            }
        }

        /// <summary>
        /// 重置诊疗步骤
        /// Epic #1589 Phase 2 - 架构合规版本
        /// </summary>
        /// <param name="id">医案ID</param>
        [HttpPut("{id}/reset-consultation-steps")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> ResetConsultationSteps(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "医案ID");
                if (validationResult != null) return validationResult;

                var result = await _medicalCaseService.ResetConsultationStepsAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "重置诊疗步骤", new { MedicalCaseId = id });
            }
        }

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        /// <param name="id">医案ID</param>
        [HttpDelete("{id}/prescription/clear")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> ClearPrescription(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "医案ID");
                if (validationResult != null) return validationResult;

                var result = await _medicalCaseService.ClearPrescriptionAsync(id);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "清空处方内容", new { MedicalCaseId = id });
            }
        }

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="formulaId">配方ID</param>
        [HttpPost("{id}/prescription/import-formula/{formulaId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(
            Guid id,
            Guid formulaId)
        {
            try
            {
                var validationResult = ValidateGuid<PrescriptionDto>(id, "医案ID");
                if (validationResult != null) return validationResult;

                var formulaValidation = ValidateGuid<PrescriptionDto>(formulaId, "配方ID");
                if (formulaValidation != null) return formulaValidation;

                var result = await _medicalCaseService.ImportFormulaIntoPrescriptionAsync(id, formulaId);
                return HandleServiceResult(result);
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "从配方导入处方", new { MedicalCaseId = id, FormulaId = formulaId });
            }
        }

        /// <summary>
        /// 暂存病案（保存当前状态，不完成整个流程）
        /// Epic #1589 Phase 5 - 架构合规版本
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="dto">病案更新信息</param>
        [HttpPut("{id}/save-as-draft")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> SaveAsDraft(
            Guid id,
            [FromBody] MedicalCaseUpdateDto dto)
        {
            try
            {
                var validationResult = ValidateGuid<MedicalCaseDto>(id, "医案ID");
                if (validationResult != null) return validationResult;

                // 调用现有的UpdateAsync方法（已符合架构）
                var result = await _medicalCaseService.UpdateAsync(id, dto);
                return HandleServiceResult(result, "病案已暂存");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "暂存病案", new { MedicalCaseId = id, dto });
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
