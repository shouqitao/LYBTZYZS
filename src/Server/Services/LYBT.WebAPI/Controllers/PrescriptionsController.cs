using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方管理 API - 基础CRUD功能
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
        /// 获取处方列表 - 支持分页和查询（Issue #1163: 扩展日期范围筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        [HttpGet]
        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "PrescriptionsCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<PrescriptionDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<PrescriptionDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var pagedResult = await _service.GetPagedAsync(page, pageSize, keyword, startDate, endDate);
                return HandlePagedServiceResult(pagedResult, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<PrescriptionDto>(ex, "获取处方列表", new { page, pageSize, keyword, startDate, endDate });
            }
        }

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
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

                return Success(result.Data, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "获取处方详情", id);
            }
        }

        /// <summary>
        /// 新增处方（已废弃）
        /// </summary>
        /// <remarks>
        /// ⚠️ 已废弃：请使用 POST /api/medicalcases/with-details 创建完整病案（含处方）。Prescription模块仅提供查询和辅助功能。
        /// </remarks>
        [HttpPost]
        [Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案（含处方）。Prescription模块仅提供查询和辅助功能。", true)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Add([FromBody] PrescriptionCreateDto dto)
        {
            try
            {
                var validationResult = ValidateModel<PrescriptionDto>();
                if (validationResult != null)
                {
                    return validationResult;
                }

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
        /// 编辑处方（已废弃）
        /// </summary>
        /// <remarks>
        /// ⚠️ 已废弃：请使用 PUT /api/medicalcases/{id}/prescription 更新处方信息。Prescription模块仅提供查询和辅助功能。
        /// </remarks>
        [HttpPut("{id}")]
        [Obsolete("请使用 PUT /api/medicalcases/{id}/prescription 更新处方信息。Prescription模块仅提供查询和辅助功能。", true)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Update(Guid id, [FromBody] PrescriptionUpdateDto dto)
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

                // 使用路由参数中的ID
                var result = await _service.UpdateAsync(id, dto);
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "编辑处方失败", ApiErrorCodes.DATAUPDATEFAILED);
                }

                LogOperation("编辑处方成功", result.Data, id);
                return Success(result.Data, "处方更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "编辑处方", new { id, dto });
            }
        }

        /// <summary>
        /// 物理删除处方（永久删除，不可恢复）
        /// Issue #1593 - Phase 4
        /// </summary>
        /// <param name="id">处方ID（与MedicalCaseId共享主键，1:1:1关系）</param>
        /// <remarks>
        /// 注意：此操作不可恢复，请谨慎使用。
        /// 建议：优先使用软删除（DELETE /api/prescriptions/{id}/soft）。
        /// 架构说明：虽然参数名为id，但由于1:1:1关系，MedicalCase.Id == Consultation.Id == Prescription.Id。
        /// </remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> PhysicalDelete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.PhysicalDeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                LogOperation("物理删除处方成功", null, id);
                return Success("处方已永久删除");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "物理删除处方", id);
            }
        }

        /// <summary>
        /// 软删除处方（标记为已删除，保留数据）
        /// Issue #1593 - Phase 4
        /// </summary>
        /// <param name="id">处方ID（与MedicalCaseId共享主键，1:1:1关系）</param>
        /// <remarks>
        /// 软删除将IsDeleted字段设置为true，数据仍保留在数据库中，可用于追溯和恢复。
        /// 架构说明：虽然参数名为id，但由于1:1:1关系，MedicalCase.Id == Consultation.Id == Prescription.Id。
        /// </remarks>
        [HttpDelete("{id}/soft")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse>> SoftDelete(Guid id)
        {
            try
            {
                var validationResult = ValidateGuid(id, "处方ID");
                if (validationResult != null)
                {
                    return validationResult;
                }

                var result = await _service.DeleteAsync(id);
                if (!result.IsSuccess)
                {
                    return NotFound("处方不存在", ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                LogOperation("软删除处方成功", null, id);
                return Success("处方已标记为删除");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "软删除处方", id);
            }
        }

        #region Issue #1163: 新增功能

        /// <summary>
        /// 生成处方编号 (Issue #1163)
        /// </summary>
        [HttpGet("generate-no")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<ActionResult<ApiResponse<string>>> GeneratePrescriptionNo()
        {
            try
            {
                var result = await _service.GeneratePrescriptionNoAsync();
                return HandleServiceResult(result, "生成成功");
            }
            catch (Exception ex)
            {
                return HandleException<string>(ex, "生成处方编号");
            }
        }

        /// <summary>
        /// 获取处方统计数据（已废弃 - MVP过度开发）
        /// </summary>
        /// <remarks>
        /// ⚠️ 已废弃：统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。
        /// </remarks>
        [HttpGet("statistics")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionMainStatisticsDto>), 200)]
        [Obsolete("统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。", true)]
        public async Task<ActionResult<ApiResponse<PrescriptionMainStatisticsDto>>> GetStatistics()
        {
            try
            {
                var result = await _service.GetStatisticsAsync();
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionMainStatisticsDto>(ex, "获取处方统计");
            }
        }

        /// <summary>
        /// 获取日期范围统计（已废弃 - MVP过度开发）
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <remarks>
        /// ⚠️ 已废弃：统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。
        /// </remarks>
        [HttpGet("statistics/range")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionRangeStatisticsDto>), 200)]
        [ProducesResponseType(400)]
        [Obsolete("统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。", true)]
        public async Task<ActionResult<ApiResponse<PrescriptionRangeStatisticsDto>>> GetRangeStatistics(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate > endDate)
                {
                    return ValidationFail<PrescriptionRangeStatisticsDto>("开始日期不能晚于结束日期");
                }

                var result = await _service.GetRangeStatisticsAsync(startDate, endDate);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionRangeStatisticsDto>(ex, "获取日期范围统计", new { startDate, endDate });
            }
        }


        /// <summary>
        /// 克隆处方（旧版） - 复制处方到同一病历 (Issue #1167)
        /// 已弃用，请使用 ClonePrescriptionTo
        /// </summary>
        /// <param name="id">原处方ID</param>
        /// <returns>新创建的处方副本</returns>
        [HttpPost("{id}/copy")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        [Obsolete("请使用 POST /prescriptions/{id}/clone-to/{targetConsultationId} 替代")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CopyPrescription(Guid id)
        {
            try
            {
                var validation = ValidateGuid<PrescriptionDto>(id, "处方ID");
                if (validation != null)
                {
                    return validation;
                }

                #pragma warning disable CS0618 // 类型或成员已过时
                var result = await _service.CloneAsync(id);
                #pragma warning restore CS0618

                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PrescriptionDto>(
                        result.ErrorMessage ?? "处方不存在",
                        ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                // 记录操作日志
                LogOperation("克隆处方（同一病历）",
                    new { OriginalId = id, NewId = result.Data.Id },
                    result.Data.Id);

                return Success(result.Data, "处方克隆成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "克隆处方", id);
            }
        }

        /// <summary>
        /// 克隆处方到指定病案 - 支持从历史处方复制 (Issue #1373 ENTRY-15, Issue #1477 架构纠正v2)
        /// </summary>
        /// <param name="sourcePrescriptionId">源处方ID</param>
        /// <param name="targetMedicalCaseId">目标病案ID（修改：原为targetConsultationId）</param>
        /// <returns>新创建的处方副本</returns>
        /// <remarks>
        /// 架构变更说明（Issue #1477）：
        /// - 参数由targetConsultationId改为targetMedicalCaseId
        /// - 通过MedicalCase聚合根更新处方（保持聚合根边界）
        /// - MedicalCase.Id == Consultation.Id == Prescription.Id（1:1:1共享主键）
        /// </remarks>
        [HttpPost("{sourcePrescriptionId}/clone-to-medicalcase/{targetMedicalCaseId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ClonePrescriptionToMedicalCase(
            Guid sourcePrescriptionId,
            Guid targetMedicalCaseId)
        {
            try
            {
                var sourceValidation = ValidateGuid<PrescriptionDto>(sourcePrescriptionId, "源处方ID");
                if (sourceValidation != null)
                {
                    return sourceValidation;
                }

                var targetValidation = ValidateGuid<PrescriptionDto>(targetMedicalCaseId, "目标病案ID");
                if (targetValidation != null)
                {
                    return targetValidation;
                }

                // TODO (#1477 Phase 1): 当前仍使用旧Service方法（参数为ConsultationId）
                // 因为1:1:1关系，MedicalCaseId == ConsultationId，暂时兼容
                // Phase 2需要调整Service层，通过MedicalCaseService更新处方
                var result = await _service.ClonePrescriptionAsync(sourcePrescriptionId, targetMedicalCaseId);

                if (!result.IsSuccess || result.Data == null)
                {
                    return NotFound<PrescriptionDto>(
                        result.ErrorMessage ?? "克隆处方失败",
                        ApiErrorCodes.PRESCRIPTIONNOTFOUND);
                }

                // 记录操作日志
                LogOperation("克隆处方到病案",
                    new { SourcePrescriptionId = sourcePrescriptionId, TargetMedicalCaseId = targetMedicalCaseId, NewPrescriptionId = result.Data.Id },
                    result.Data.Id);

                return Success(result.Data, "处方克隆成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "克隆处方到病案", new { sourcePrescriptionId, targetMedicalCaseId });
            }
        }

        /// <summary>
        /// 搜索处方 - 按患者姓名或症状/诊断关键字 (Issue #1372 ENTRY-14)
        /// </summary>
        /// <param name="patientName">患者姓名关键字（可空）</param>
        /// <param name="symptomKeyword">症状/诊断关键字（可空）</param>
        /// <returns>处方搜索结果列表</returns>
        [HttpGet("search")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
            [FromQuery] string? patientName = null,
            [FromQuery] string? symptomKeyword = null)
        {
            try
            {
                // 至少需要一个搜索条件
                if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptomKeyword))
                {
                    return ValidationFail<List<PrescriptionSearchResultDto>>("请至少提供一个搜索条件（患者姓名或症状关键字）");
                }

                var result = await _service.SearchPrescriptionsAsync(patientName, symptomKeyword);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex, "搜索处方", new { patientName, symptomKeyword });
            }
        }

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13, Issue #1374 ENTRY-16)
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5条）</param>
        /// <returns>患者最近处方列表</returns>
        [HttpGet("patient/{patientId}/recent")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "patientId", "count" })]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionSearchResultDto>>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> GetPatientRecentPrescriptions(
            Guid patientId,
            [FromQuery] int count = 5)
        {
            try
            {
                var validation = ValidateGuid<List<PrescriptionSearchResultDto>>(patientId, "患者ID");
                if (validation != null)
                {
                    return validation;
                }

                if (count <= 0 || count > 100)
                {
                    return ValidationFail<List<PrescriptionSearchResultDto>>("返回数量必须在1-100之间");
                }

                var result = await _service.GetPatientRecentPrescriptionsAsync(patientId, count);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<List<PrescriptionSearchResultDto>>(ex, "获取患者最近处方", new { patientId, count });
            }
        }

        /// <summary>
        /// 导入验方到处方 (Issue #1366 ENTRY-8, Issue #1367 ENTRY-9)
        /// 从已验证的验方批量导入药材，并记录引用的验方名称
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="formulaId">验方ID</param>
        /// <returns>更新后的处方DTO</returns>
        [HttpPost("{prescriptionId}/import-formula/{formulaId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ImportFormulaIntoPrescription(
            Guid prescriptionId,
            Guid formulaId)
        {
            try
            {
                var prescriptionValidation = ValidateGuid<PrescriptionDto>(prescriptionId, "处方ID");
                if (prescriptionValidation != null)
                {
                    return prescriptionValidation;
                }

                var formulaValidation = ValidateGuid<PrescriptionDto>(formulaId, "验方ID");
                if (formulaValidation != null)
                {
                    return formulaValidation;
                }

                var result = await _service.ImportFormulaIntoPrescriptionAsync(prescriptionId, formulaId);

                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(
                        result.ErrorMessage ?? "导入验方失败",
                        ApiErrorCodes.DATASAVEFAILED);
                }

                // 记录操作日志
                LogOperation("导入验方到处方",
                    new { PrescriptionId = prescriptionId, FormulaId = formulaId },
                    prescriptionId);

                return Success(result.Data, result.Message ?? "验方导入成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "导入验方到处方", new { prescriptionId, formulaId });
            }
        }

        #endregion
    }
}
