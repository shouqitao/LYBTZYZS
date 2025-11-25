using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCase.Dtos;     // MedicalCasePrescriptionDto, SetPrescriptionFlagRequest (模块专用)
using LYBT.Module.MedicalCase.Interfaces; // CanEditResponse, CanDeleteResponse
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalCaseDto = LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDto;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
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
        /// Issue #2212: 提取当前医生ID并传递给Service层
        /// Epic #2210 Phase 3 P0 Bug修复: Entity→DTO映射避免枚举转换错误
        /// </summary>
        /// <param name="request">创建请求</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> CreateMedicalCase(
            [FromBody] CreateMedicalCaseRequest request)
        {
            try
            {
                // Issue #2212: 获取当前医生ID
                var (doctorId, _, _) = GetOperator();

                var entity = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate, doctorId);

                if (entity == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("患者不存在"));

                _logger.LogInformation("病案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                    entity.Id, entity.DoctorName, entity.PatientName);

                // Epic #2210 Phase 3 P0 Bug修复: Entity → MedicalCaseDto 映射
                var dto = new MedicalCaseDto
                {
                    Id = entity.Id,
                    PatientId = entity.PatientId,
                    PatientName = entity.PatientName,
                    DoctorId = entity.DoctorId,
                    DoctorName = entity.DoctorName,
                    ConsultationDate = entity.ConsultationDate,
                    CaseStatus = entity.CaseStatus,
                    Remark = entity.Remark,
                    Diagnosis = entity.Consultation?.TCMDiagnosis,
                    Status = entity.Status, // 系统状态（CommonStatus）
                    CreatedAt = entity.CreatedAt
                };

                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "病案创建成功"));
            }
            catch (ArgumentException ex)
            {
                // DoctorId参数验证失败
                _logger.LogWarning(ex, "创建病案失败：参数验证失败");
                return BadRequest(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                // BR-001: 单个患者只能有一个Active病案
                _logger.LogWarning(ex, "创建病案失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "创建病案", request);
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
                // Issue #2241: 使用UserRole枚举比较
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

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
                // Issue #2241: 使用UserRole枚举比较
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

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
                // Issue #2241: 使用UserRole枚举比较
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

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
                // Issue #2241: 使用UserRole枚举比较
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

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
        /// 获取完整的医疗案例（包含所有关联数据）
        /// Epic #2210 Phase 3 P0 Bug修复: 补充缺失的API端点
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>完整的病案详情（包含Consultation和Prescription）</returns>
        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// Epic #2210 Phase 3 P0 Bug修复: 补充缺失的API端点
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>完整的病案详情（包含Consultation和Prescription）</returns>
        [HttpGet("{id}/with-details")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetMedicalCaseByIdWithDetails(Guid id)
        {
            try
            {
                var entity = await _medicalCaseService.GetByIdAsync(id);

                if (entity == null)
                    return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("病案不存在"));

                // Entity → MedicalCaseDetailDto 映射
                var detailDto = new MedicalCaseDetailDto
                {
                    // 基础字段（继承自MedicalCaseDto）
                    Id = entity.Id,
                    PatientId = entity.PatientId,
                    PatientName = entity.PatientName,
                    DoctorId = entity.DoctorId,
                    DoctorName = entity.DoctorName,
                    ConsultationDate = entity.ConsultationDate,
                    CaseStatus = entity.CaseStatus,
                    Remark = entity.Remark,
                    Diagnosis = entity.Consultation?.TCMDiagnosis,
                    Status = entity.Status, // 系统状态（CommonStatus）
                    CreatedAt = entity.CreatedAt,

                    // 详细字段（MedicalCaseDetailDto扩展）
                    ChiefComplaint = entity.Consultation?.ChiefComplaint,
                    PresentIllness = entity.Consultation?.PresentIllness,
                    DiagnosisResult = entity.Consultation?.TCMDiagnosis,
                    TreatmentPlan = entity.Consultation?.TreatmentPrinciple,

                    // 关联数据
                    Consultation = entity.Consultation != null ? new ConsultationDto
                    {
                        Id = entity.Consultation.Id,
                        MedicalCaseId = entity.Id, // 使用医案ID（共享主键）
                        PatientId = entity.PatientId,
                        UserId = entity.DoctorId,
                        PatientName = entity.PatientName,
                        DoctorName = entity.DoctorName,
                        ChiefComplaint = entity.Consultation.ChiefComplaint,
                        PresentIllness = entity.Consultation.PresentIllness,
                        Inspection = entity.Consultation.Inspection,
                        AuscultationOlfaction = entity.Consultation.AuscultationOlfaction,
                        Inquiry = entity.Consultation.Inquiry,
                        Palpation = entity.Consultation.Palpation,
                        TCMDiagnosis = entity.Consultation.TCMDiagnosis,
                        TreatmentPrinciple = entity.Consultation.TreatmentPrinciple,
                        MedicalAdvice = entity.Consultation.MedicalAdvice,
                        Step1CompletedAt = entity.Consultation.Step1CompletedAt,
                        Step2CompletedAt = entity.Consultation.Step2CompletedAt,
                        Remark = entity.Consultation.Remark,
                        Status = (CommonStatus)(int)entity.Consultation.Status,
                        CreatedAt = entity.Consultation.CreatedAt,
                        UpdatedAt = entity.Consultation.UpdatedAt
                    } : null,

                    Prescription = entity.Prescription != null ? new PrescriptionDto
                    {
                        Id = entity.Prescription.Id,
                        MedicalCaseId = entity.Id,
                        PatientId = entity.PatientId,
                        UserId = entity.DoctorId,
                        PrescriptionNumber = entity.Prescription.PrescriptionNumber,
                        Indication = entity.Prescription.Indication,
                        DosageCount = entity.Prescription.DosageCount,
                        Usage = null, // 实体没有Usage字段，使用null
                        Discount = entity.Prescription.Discount,
                        Advice = entity.Prescription.Advice,
                        FormulaSource = entity.Prescription.FormulaSource,
                        ReferencedFormulas = entity.Prescription.ReferencedFormulas,
                        Remark = entity.Prescription.Remark,
                        Items = entity.Prescription.Items?.Select(item => new PrescriptionItemDto
                        {
                            Id = item.Id,
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Dosage = item.Quantity, // Entity用Quantity，DTO用Dosage
                            TotalPrice = item.Amount, // Entity用Amount（计算属性），DTO用TotalPrice
                            TotalWeight = item.Quantity, // 总重量=用量
                            Subtotal = item.Amount, // Entity用Amount，DTO用Subtotal
                            Usage = item.Usage,
                            Remark = item.Remark
                        }).ToList() ?? new List<PrescriptionItemDto>(),
                        // 计算属性（Entity没有这些字段，需要在映射时计算）
                        SingleDosePrice = entity.Prescription.Items?.Sum(x => x.Amount) ?? 0, // 单剂价格=所有药材小计之和
                        TotalPrice = (entity.Prescription.Items?.Sum(x => x.Amount) ?? 0) * entity.Prescription.DosageCount * entity.Prescription.Discount, // 总价=单剂×帖数×折扣
                        TotalWeight = entity.Prescription.Items?.Sum(x => x.Quantity) ?? 0, // 总重量=所有药材用量之和
                        Status = (CommonStatus)(int)entity.Prescription.Status,
                        CreatedAt = entity.Prescription.CreatedAt,
                        UpdatedAt = entity.Prescription.UpdatedAt
                    } : null
                };

                return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(detailDto, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDetailDto>(ex, "获取病案详情（含关联数据）", new { id });
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
                    CaseStatus = entity.CaseStatus,
                    Remark = entity.Remark,
                    Diagnosis = entity.Consultation?.TCMDiagnosis, // 关联查询诊断信息
                    Status = entity.Status, // 系统状态（CommonStatus）
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
        /// Epic #2210 Task 3.1.3: 添加doctorId筛选，支持可选参数
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">可选医生ID（未传递时使用当前登录医生ID）</param>

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 添加缺失的API端点
        /// 业务规则：返回当前医生的所有Active状态医案的患者信息
        /// </summary>
        /// <param name="doctorId">医生ID（可选，默认使用当前登录医生ID）</param>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 403)]
        public async Task<ActionResult<ApiResponse<List<PendingMedicalCaseDto>>>> GetPendingCases()
        {
            try
            {
                List<PendingMedicalCaseDto> result;
                try
                {
                    var (operatorId, operatorName, operatorRole) = GetOperator();

                    // Issue #2241: 根据角色判断查询范围，使用UserRole枚举比较
                    if (operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin)
                    {
                        // 管理员查询所有待诊医案
                        _logger.LogInformation("管理员查询全部待诊队列，OperatorId: {OperatorId}, Role: {Role}",
                            operatorId, operatorRole);
                        result = await _medicalCaseService.GetAllPendingCasesAsync();
                    }
                    else if (operatorRole == UserRole.Doctor)
                    {
                        // 医生只查询自己的待诊医案
                        _logger.LogInformation("医生查询自己的待诊队列，DoctorId: {DoctorId}",
                            operatorId);
                        result = await _medicalCaseService.GetPendingCasesAsync(operatorId);
                    }
                    else
                    {
                        _logger.LogWarning("无权限的用户尝试查询待诊队列，OperatorId: {OperatorId}, Role: {Role}",
                            operatorId, operatorRole);
                        return Forbid();
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized(ApiResponse<List<PendingMedicalCaseDto>>.CreateFail("未登录或用户信息无效"));
                }

                _logger.LogInformation("待诊队列查询成功，Count: {Count}", result.Count);

                return Ok(ApiResponse<List<PendingMedicalCaseDto>>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<List<PendingMedicalCaseDto>>(ex, "获取待诊队列", null);
            }
        }

        [HttpGet("patient/{patientId}/unfinished")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 403)]
        public async Task<ActionResult<ApiResponse<MedicalCaseDto>>> GetUnfinishedCaseByPatientId(
            Guid patientId,
            [FromQuery] Guid? doctorId = null)
        {
            try
            {
                // Epic #2210 Task 3.1.3: Q4医生筛选链 - 提取当前医生ID
                Guid currentDoctorId;
                try
                {
                    var (operatorId, operatorName, operatorRole) = GetOperator();

                    // 如果未传递doctorId，使用当前登录医生ID
                    if (doctorId == null || doctorId == Guid.Empty)
                    {
                        // Issue #2241: 验证当前用户是医生角色，使用UserRole枚举比较
                        if (operatorRole != UserRole.Doctor)
                        {
                            _logger.LogWarning("非医生用户尝试查询未完成医案，OperatorId: {OperatorId}, Role: {Role}",
                                operatorId, operatorRole);
                            return Forbid();
                        }
                        currentDoctorId = operatorId;
                    }
                    else
                    {
                        // 传递了doctorId（管理员扩展），直接使用
                        currentDoctorId = doctorId.Value;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized(ApiResponse<MedicalCaseDto>.CreateFail("未登录或用户信息无效"));
                }

                var entityResult = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patientId, currentDoctorId);

                if (entityResult == null)
                {
                    _logger.LogDebug("未找到患者的未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                        patientId, currentDoctorId);
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("未找到该患者的未完成医案"));
                }

                // Epic #2210 Phase 3 P0 Bug修复: Entity → DTO映射
                var dtoResult = new MedicalCaseDto
                {
                    Id = entityResult.Id,
                    PatientId = entityResult.PatientId,
                    PatientName = entityResult.PatientName,
                    DoctorId = entityResult.DoctorId,
                    DoctorName = entityResult.DoctorName,
                    ConsultationDate = entityResult.ConsultationDate,
                    CaseStatus = entityResult.CaseStatus,
                    Remark = entityResult.Remark,
                    Diagnosis = entityResult.Consultation?.TCMDiagnosis,
                    Status = entityResult.Status, // 系统状态（CommonStatus）
                    CreatedAt = entityResult.CreatedAt
                };

                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dtoResult, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseDto>(ex, "获取患者未完成医案",
                    new { patientId, doctorId });
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
