using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCase.Dtos;     // MedicalCasePrescriptionDto, SetPrescriptionFlagRequest (模块专用)
using LYBT.Module.MedicalCase.Interfaces; // CanEditResponse, CanDeleteResponse
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using MedicalCaseDto = LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDto;
// Epic #1612: 新Service接口和DTOs
using NewMedicalCaseService = LYBT.Module.MedicalCase.Interfaces.IMedicalCaseService;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API V1 - Epic #1612重构版
    /// 遵循Write/Read/Helper Layer分离原则
    /// 所有写操作通过MedicalCase聚合根
    /// 注：保持v1版本，v2升级延后到Phase 3完成后
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize]
    public class MedicalCaseController : BaseApiController
    {
        private readonly NewMedicalCaseService _medicalCaseService;

        public MedicalCaseController(
            NewMedicalCaseService medicalCaseService,
            ILogger<MedicalCaseController> logger)
            : base(logger)
        {
            _medicalCaseService = medicalCaseService;
        }

        // ========== Write Layer（写操作，通过聚合根）==========

        /// <summary>
        /// 创建新病案
        /// Epic #1612 - AR-001: 通过聚合根创建
        /// </summary>
        /// <param name="request">创建请求</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 422)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
            [FromBody] CreateMedicalCaseRequest request)
        {
            try
            {
                var result = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("患者不存在"));

                _logger.LogInformation("病案创建成功，ID: {Id}", result.Id);
                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "病案创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                // BR-001: 单个患者只能有一个Active病案
                _logger.LogWarning(ex, "创建病案失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "创建病案", request);
            }
        }

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// Epic #1612 - AR-001: 通过聚合根更新Consultation
        /// </summary>
        [HttpPut("{id}/consultation")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 400)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> UpdateConsultation(
            Guid id,
            [FromBody] ConsultationInputDto request)
        {
            try
            {
                // Epic #1731: 获取当前用户信息以进行权限检查
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole?.Contains("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

                var result = await _medicalCaseService.UpdateConsultationAsync(id, request, operatorId, isAdmin);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

                _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "辨证信息更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "更新辨证信息失败：状态不允许");
                return BadRequest(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "更新辨证信息", new { id, request });
            }
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612 - BF-002: 动态流程控制
        /// </summary>
        [HttpPut("{id}/prescription-flag")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 422)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request)
        {
            try
            {
                // Epic #1731: 获取当前用户信息以进行权限检查
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole?.Contains("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

                var result = await _medicalCaseService.SetPrescriptionFlagAsync(id, request.NeedsPrescription, operatorId, isAdmin);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

                _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                    id, request.NeedsPrescription);
                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "处方标记更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // AR-003: 已有处方时不能再标记为需要开处方
                _logger.LogWarning(ex, "处方标记更新失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "更新处方标记", new { id, request });
            }
        }

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612 - AR-001/AR-003: 通过聚合根创建，一诊一方约束
        /// </summary>
        [HttpPost("{id}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 422)]
        public async Task<ActionResult<ApiResponse<PrescriptionEntity>>> CreatePrescription(
            Guid id,
            [FromBody] PrescriptionCreateDto request)
        {
            try
            {
                var result = await _medicalCaseService.CreatePrescriptionAsync(id, request);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionEntity>.CreateFail("病案不存在"));

                _logger.LogInformation("处方创建成功，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, result.Id);
                return Ok(ApiResponse<PrescriptionEntity>.CreateSuccess(result, "处方创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                // AR-003: 一诊一方约束
                _logger.LogWarning(ex, "处方创建失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<PrescriptionEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionEntity>(ex, "创建处方", new { id, request });
            }
        }

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// Epic #1612 - AR-001: 通过聚合根更新
        /// </summary>
        [HttpPut("{id}/prescriptions/{prescriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionEntity>), 403)]
        public async Task<ActionResult<ApiResponse<PrescriptionEntity>>> UpdatePrescription(
            Guid id,
            Guid prescriptionId,
            [FromBody] PrescriptionEditDto request)
        {
            try
            {
                // Epic #1731: 获取当前用户信息以进行权限检查
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole?.Contains("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

                var result = await _medicalCaseService.UpdatePrescriptionAsync(id, prescriptionId, request, operatorId, isAdmin);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionEntity>.CreateFail("病案或处方不存在"));

                _logger.LogInformation("处方更新成功，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, prescriptionId);
                return Ok(ApiResponse<PrescriptionEntity>.CreateSuccess(result, "处方更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // 处方不属于该病案
                _logger.LogWarning(ex, "处方更新失败：验证失败");
                return StatusCode(403, ApiResponse<PrescriptionEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionEntity>(ex, "更新处方", new { id, prescriptionId, request });
            }
        }

        /// <summary>
        /// 删除处方（软删除）
        /// Epic #1612 - AR-001: 通过聚合根删除，修复V3违规
        /// </summary>
        [HttpDelete("{id}/prescriptions/{prescriptionId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 422)]
        public async Task<ActionResult> DeletePrescription(
            Guid id,
            Guid prescriptionId)
        {
            try
            {
                // Epic #1731: 获取当前用户信息以进行权限检查
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole?.Contains("Admin", StringComparison.OrdinalIgnoreCase) ?? false;

                var result = await _medicalCaseService.DeletePrescriptionAsync(id, prescriptionId, operatorId, isAdmin);

                if (!result)
                    return NotFound(ApiResponse.CreateFail("病案或处方不存在"));

                _logger.LogInformation("处方删除成功，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, prescriptionId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // 处方不属于该病案 或 病案已完成
                _logger.LogWarning(ex, "处方删除失败：业务规则验证失败");

                if (ex.Message.Contains("不属于"))
                    return StatusCode(403, ApiResponse.CreateFail(ex.Message));
                else
                    return UnprocessableEntity(ApiResponse.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                HandleException(ex, "删除处方", new { id, prescriptionId });
                throw; // HandleException会抛出转换后的异常
            }
        }

        /// <summary>
        /// 更新病案状态
        /// Epic #1612修正版: 支持Draft/Active/Completed/Cancelled状态流转
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 422)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> UpdateStatus(
            Guid id,
            [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var result = await _medicalCaseService.UpdateStatusAsync(id, request.Status);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

                _logger.LogInformation("病案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}",
                    id, request.Status);
                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "状态更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // 状态转换不合法
                _logger.LogWarning(ex, "状态更新失败：状态转换不合法");
                return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "更新病案状态", new { id, request });
            }
        }

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612 - BF-002: 三步流程验证
        /// </summary>
        [HttpPut("{id}/complete")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 422)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CompleteMedicalCase(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.CompleteAsync(id);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

                _logger.LogInformation("病案完成，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "病案已完成"));
            }
            catch (InvalidOperationException ex)
            {
                // BF-002: 三步流程验证失败
                _logger.LogWarning(ex, "病案完成失败：流程验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseEntity>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "完成病案", new { id });
            }
        }

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<ActionResult<ApiResponse>> CloseMedicalCase(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.CloseCaseAsync(id);

                if (!result)
                    return NotFound(ApiResponse.CreateFail("病案不存在"));

                _logger.LogInformation("病案关闭，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse.CreateSuccess("病案已关闭"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "关闭病案", new { id });
            }
        }

        // ========== Read Layer（读操作，独立查询）==========

        /// <summary>
        /// 获取病案详情
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> GetById(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.GetByIdAsync(id);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("病案不存在"));

                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "获取病案详情", new { id });
            }
        }

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseDto>>), 200)]
        public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseDto>>>> GetList(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<PagedResult<MedicalCaseDto>>.CreateFail(
                        "页码和页大小参数无效（页码>0，页大小1-100）"));
                }

                var entityResult = await _medicalCaseService.GetListAsync(status, patientId, page, pageSize);

                // Entity → DTO映射
                var dtoItems = entityResult.Items.Select(entity => new MedicalCaseDto
                {
                    Id = entity.Id,
                    PatientId = entity.PatientId,
                    PatientName = entity.PatientName,
                    DoctorId = entity.DoctorId,
                    DoctorName = entity.DoctorName,
                    ConsultationDate = entity.ConsultationDate,
                    CaseStatus = entity.Status,
                    Remark = entity.Remark,
                    Diagnosis = entity.Consultation?.TCMDiagnosis, // 关联查询诊断信息
                    Status = (CommonStatus)(int)entity.Status, // 通用状态映射
                    CreatedAt = entity.CreatedAt
                }).ToList();

                var dtoResult = new PagedResult<MedicalCaseDto>
                {
                    Items = dtoItems,
                    TotalCount = entityResult.TotalCount,
                    CurrentPage = entityResult.CurrentPage,
                    PageSize = entityResult.PageSize
                };

                return Ok(ApiResponse<PagedResult<MedicalCaseDto>>.CreateSuccess(dtoResult, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<PagedResult<MedicalCaseDto>>(ex, "获取病案列表",
                    new { status, patientId, page, pageSize });
            }
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        [HttpGet("{medicalCaseId}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetConsultationList(
            Guid medicalCaseId)
        {
            try
            {
                var result = await _medicalCaseService.GetConsultationListAsync(medicalCaseId);

                return Ok(ApiResponse<List<ConsultationDto>>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<List<ConsultationDto>>(ex, "获取辨证记录列表",
                    new { medicalCaseId });
            }
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        [HttpGet("{medicalCaseId}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCasePrescriptionDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<MedicalCasePrescriptionDto>>>> GetPrescriptionList(
            Guid medicalCaseId)
        {
            try
            {
                var result = await _medicalCaseService.GetPrescriptionListAsync(medicalCaseId);

                return Ok(ApiResponse<List<MedicalCasePrescriptionDto>>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<List<MedicalCasePrescriptionDto>>(ex, "获取处方列表",
                    new { medicalCaseId });
            }
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// </summary>
        [HttpGet("patient/{patientId}/unfinished")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseEntity>), 404)]
        public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> GetUnfinishedCaseByPatientId(
            Guid patientId)
        {
            try
            {
                var result = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patientId);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseEntity>.CreateFail("未找到该患者的未完成医案"));

                return Ok(ApiResponse<MedicalCaseEntity>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseEntity>(ex, "获取患者未完成医案",
                    new { patientId });
            }
        }

        // ========== Helper Layer（辅助功能）==========

        /// <summary>
        /// 验证病案是否可编辑
        /// Epic #1612: 检查病案状态和权限
        /// Phase 2.3: 此端点已标记为过时，将在v2.0移除
        /// 推荐：使用GetById返回的Status字段进行客户端判断（medicalCase.Status == MedicalCaseStatus.Active）
        /// </summary>
        [Obsolete("此端点将在v2.0移除，请使用GetById返回的Status字段判断是否可编辑", false)]
        [HttpGet("{id}/can-edit")]
        [ProducesResponseType(typeof(ApiResponse<CanEditResponse>), 200)]
        public async Task<ActionResult<ApiResponse<CanEditResponse>>> CanEdit(Guid id)
        {
            try
            {
                var result = await _medicalCaseService.CanEditAsync(id);

                return Ok(ApiResponse<CanEditResponse>.CreateSuccess(result, "验证成功"));
            }
            catch (Exception ex)
            {
                return HandleException<CanEditResponse>(ex, "验证病案可编辑性", new { id });
            }
        }

        /// <summary>
        /// 验证处方是否可删除
        /// Epic #1612: 检查处方打印状态
        /// Phase 2.3: 此端点已标记为过时，将在v2.0移除
        /// 推荐：使用GetById返回的Prescription.IsPrinted字段进行客户端判断（medicalCase.Prescription?.IsPrinted == false）
        /// </summary>
        [Obsolete("此端点将在v2.0移除，请使用GetById返回的Prescription.IsPrinted字段判断是否可删除", false)]
        [HttpGet("{id}/prescriptions/{prescriptionId}/can-delete")]
        [ProducesResponseType(typeof(ApiResponse<CanDeleteResponse>), 200)]
        public async Task<ActionResult<ApiResponse<CanDeleteResponse>>> CanDeletePrescription(
            Guid id,
            Guid prescriptionId)
        {
            try
            {
                var result = await _medicalCaseService.CanDeletePrescriptionAsync(id, prescriptionId);

                return Ok(ApiResponse<CanDeleteResponse>.CreateSuccess(result, "验证成功"));
            }
            catch (Exception ex)
            {
                return HandleException<CanDeleteResponse>(ex, "验证处方可删除性",
                    new { id, prescriptionId });
            }
        }
    }

    // ========== Request DTOs ==========

    /// <summary>
    /// 创建病案请求
    /// </summary>
    public class CreateMedicalCaseRequest
    {
        /// <summary>患者ID</summary>
        public Guid PatientId { get; set; }

        /// <summary>就诊日期</summary>
        public DateTime VisitDate { get; set; }
    }

    /// <summary>
    /// 标记是否开处方请求
    /// </summary>
    public class SetPrescriptionFlagRequest
    {
        /// <summary>是否需要开处方</summary>
        public bool NeedsPrescription { get; set; }
    }

    /// <summary>
    /// 更新病案状态请求
    /// Epic #1612修正版
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>目标状态：Draft/Active/Completed/Cancelled</summary>
        public MedicalCaseStatus Status { get; set; }
    }
}
