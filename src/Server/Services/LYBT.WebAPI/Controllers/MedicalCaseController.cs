using Asp.Versioning;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalCaseDto = LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDto;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API V1 - Epic #1612重构版
    /// 遵循CQRS原则：Command/Query/State服务分离
    /// 所有写操作通过MedicalCase聚合根
    /// Phase 3: 拆分为三个职责单一的Service
    /// </summary>
    /// optimize-api-permissions: 医案管理需Doctor或Admin角色
    /// 资源级授权通过MedicalCaseAuthorizationHandler实现
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = "DoctorOrAdmin")]
    public class MedicalCaseController : BaseApiController
    {
        private readonly IMedicalCaseCommandService _commandService;
        private readonly IMedicalCaseQueryService _queryService;
        private readonly IMedicalCaseStateService _stateService;
        private readonly IMedicalCasePermissionService _permissionService;
        private readonly IMedicalCaseAuditService _auditService;
        private readonly IAuthorizationService _authorizationService;

        public MedicalCaseController(
            IMedicalCaseCommandService commandService,
            IMedicalCaseQueryService queryService,
            IMedicalCaseStateService stateService,
            IMedicalCasePermissionService permissionService,
            IMedicalCaseAuditService auditService,
            IAuthorizationService authorizationService,
            ILogger<MedicalCaseController> logger)
            : base(logger)
        {
            _commandService = commandService;
            _queryService = queryService;
            _stateService = stateService;
            _permissionService = permissionService;
            _auditService = auditService;
            _authorizationService = authorizationService;
        }

        // ========== Write Layer（写操作，通过聚合根）==========

        /// <summary>
        /// 创建新病案
        /// Epic #1612 - AR-001: 通过聚合根创建
        /// Issue #2212: 提取当前医生ID并传递给Service层
        /// Epic #2210 Phase 3 P0 Bug修复: Entity→DTO映射避免枚举转换错误
        /// optimize-api-permissions: 只有Doctor可以创建新病案，Admin不能创建
        /// </summary>
        /// <param name="request">创建请求</param>
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        public async Task<IActionResult> CreateMedicalCase(
            [FromBody] CreateMedicalCaseRequest request)
        {
            try
            {
                // Issue #2212: 获取当前医生ID
                var (doctorId, _, _) = GetOperator();

                var entity = await _commandService.CreateAsync(request.PatientId, request.VisitDate, doctorId);

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
                    CreatedAt = entity.CreatedAt,
                    // Issue #2231: 添加ConsultationId字段（共享主键，值等于MedicalCase.Id）
                    ConsultationId = entity.Id
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
            catch (DbUpdateException dbEx)
            {
                // 数据库约束错误 - 提取详细错误信息以便诊断
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "创建病案失败：数据库约束错误 - {InnerMessage}", innerMessage);
                return BadRequest(ApiResponse<MedicalCaseDto>.CreateFail($"数据库错误: {innerMessage}"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建病案", request);
            }
        }

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// Epic #1612 - AR-001: 通过聚合根更新Consultation
        /// Bug Fix: 返回ConsultationDto以匹配客户端期望类型
        /// refactor-authorization-system: AUTHZ-002 使用IAuthorizationService进行资源级授权
        /// </summary>
        [HttpPut("{id}/consultation")]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 403)]
        public async Task<IActionResult> UpdateConsultation(
            Guid id,
            [FromBody] ConsultationInputDto request)
        {
            try
            {
                // refactor-authorization-system: 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse<ConsultationDto>.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权编辑病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse<ConsultationDto>.CreateFail("无权编辑此病案"));
                }

                // Epic #1731: 获取当前用户信息
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                var result = await _commandService.UpdateConsultationAsync(id, request, operatorId, isAdmin);

                // Bug Fix: 转换为ConsultationDto以匹配客户端期望
                var consultationDto = result?.Consultation != null ? new ConsultationDto
                {
                    Id = result.Consultation.Id,
                    MedicalCaseId = result.Id,
                    PatientId = result.PatientId,
                    UserId = result.DoctorId,
                    ChiefComplaint = result.Consultation.ChiefComplaint,
                    PresentIllness = result.Consultation.PresentIllness,
                    Inspection = result.Consultation.Inspection,
                    AuscultationOlfaction = result.Consultation.AuscultationOlfaction,
                    Inquiry = result.Consultation.Inquiry,
                    Palpation = result.Consultation.Palpation,
                    TCMDiagnosis = result.Consultation.TCMDiagnosis,
                    TreatmentPrinciple = result.Consultation.TreatmentPrinciple,
                    MedicalAdvice = result.Consultation.MedicalAdvice,
                    Remark = result.Consultation.Remark,
                    // DD-002: 移除Status字段，Consultation状态从聚合根MedicalCase派生
                    CreatedAt = result.Consultation.CreatedAt,
                    UpdatedAt = result.Consultation.UpdatedAt
                } : null;

                _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse<ConsultationDto>.CreateSuccess(consultationDto, "辨证信息更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "更新辨证信息失败：状态不允许");
                return BadRequest(ApiResponse<ConsultationDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新辨证信息", new { id, request });
            }
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612 - BF-002: 动态流程控制
        /// refactor-authorization-system: AUTHZ-002 使用IAuthorizationService进行资源级授权
        /// </summary>
        [HttpPut("{id}/prescription-flag")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 403)]
        public async Task<IActionResult> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request)
        {
            try
            {
                // refactor-authorization-system: 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权编辑病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse<MedicalCaseDto>.CreateFail("无权编辑此病案"));
                }

                // Epic #1731: 获取当前用户信息
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                var result = await _commandService.SetPrescriptionFlagAsync(id, request.NeedsPrescription, operatorId, isAdmin);
                if (result == null)
                {
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));
                }

                // Entity → DTO映射
                var dto = MapToMedicalCaseDto(result);

                _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                    id, request.NeedsPrescription);
                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "处方标记更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // AR-003: 已有处方时不能再标记为需要开处方
                _logger.LogWarning(ex, "处方标记更新失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新处方标记", new { id, request });
            }
        }

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612 - AR-001/AR-003: 通过聚合根创建，一诊一方约束
        /// </summary>
        [HttpPost("{id}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 422)]
        public async Task<IActionResult> CreatePrescription(
            Guid id,
            [FromBody] PrescriptionCreateDto request)
        {
            try
            {
                var result = await _commandService.CreatePrescriptionAsync(id, request);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionDto>.CreateFail("病案不存在"));

                // Entity → DTO映射
                var dto = MapToPrescriptionDto(result, id);

                _logger.LogInformation("处方创建成功，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, result.Id);
                return Ok(ApiResponse<PrescriptionDto>.CreateSuccess(dto, "处方创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                // AR-003: 一诊一方约束
                _logger.LogWarning(ex, "处方创建失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<PrescriptionDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建处方", new { id, request });
            }
        }

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// Epic #1612 - AR-001: 通过聚合根更新
        /// </summary>
        [HttpPut("{id}/prescriptions/{prescriptionId}")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 403)]
        public async Task<IActionResult> UpdatePrescription(
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

                var result = await _commandService.UpdatePrescriptionAsync(id, prescriptionId, request, operatorId, isAdmin);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionDto>.CreateFail("病案或处方不存在"));

                // Entity → DTO映射
                var dto = MapToPrescriptionDto(result, id);

                _logger.LogInformation("处方更新成功，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, prescriptionId);
                return Ok(ApiResponse<PrescriptionDto>.CreateSuccess(dto, "处方更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // 处方不属于该病案
                _logger.LogWarning(ex, "处方更新失败：验证失败");
                return StatusCode(403, ApiResponse<PrescriptionDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新处方", new { id, prescriptionId, request });
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

                var result = await _commandService.DeletePrescriptionAsync(id, prescriptionId, operatorId, isAdmin);

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

        #region 简化处方端点（一诊一方模式）

        /// <summary>
        /// 创建处方（简化端点，单数路由）
        /// Bug Fix: 匹配客户端API接口 POST /api/v1/medicalcases/{id}/prescription
        /// </summary>
        [HttpPost("{id}/prescription")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 422)]
        public async Task<IActionResult> CreatePrescriptionSimple(
            Guid id,
            [FromBody] PrescriptionCreateDto request)
        {
            try
            {
                var result = await _commandService.CreatePrescriptionAsync(id, request);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionDto>.CreateFail("病案不存在"));

                // Bug Fix: 转换为PrescriptionDto以匹配客户端期望
                var dto = MapToPrescriptionDto(result, id);

                _logger.LogInformation("处方创建成功(简化端点)，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, result.Id);
                return Ok(ApiResponse<PrescriptionDto>.CreateSuccess(dto, "处方创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "处方创建失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<PrescriptionDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建处方", new { id, request });
            }
        }

        /// <summary>
        /// 更新处方（简化端点，单数路由，自动获取prescriptionId）
        /// Bug Fix: 匹配客户端API接口 PUT /api/v1/medicalcases/{id}/prescription
        /// </summary>
        [HttpPut("{id}/prescription")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 403)]
        public async Task<IActionResult> UpdatePrescriptionSimple(
            Guid id,
            [FromBody] PrescriptionUpdateDto request)
        {
            try
            {
                // Epic #1731: 获取当前用户信息以进行权限检查
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                // 一诊一方模式：从医案获取处方ID
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase?.Prescription == null)
                    return NotFound(ApiResponse<PrescriptionDto>.CreateFail("病案或处方不存在"));

                var prescriptionId = medicalCase.Prescription.Id;

                // PrescriptionUpdateDto已经包含正确的属性，直接转换为PrescriptionEditDto
                var editDto = new PrescriptionEditDto
                {
                    Id = prescriptionId,
                    DosageCount = request.DosageCount,
                    Items = request.Items?.Select(i => new PrescriptionItemInputDto
                    {
                        Id = i.Id,
                        HerbId = i.HerbId,
                        HerbName = i.HerbName,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Dosage = i.Dosage,
                        UnitPrice = i.UnitPrice,
                        Subtotal = i.Subtotal,
                        Usage = i.Usage,
                        Remark = i.Remark
                    }).ToList() ?? new List<PrescriptionItemInputDto>()
                };

                var result = await _commandService.UpdatePrescriptionAsync(id, prescriptionId, editDto, operatorId, isAdmin);

                if (result == null)
                    return NotFound(ApiResponse<PrescriptionDto>.CreateFail("病案或处方不存在"));

                // Bug Fix: 转换为PrescriptionDto以匹配客户端期望
                var dto = MapToPrescriptionDto(result, id);

                _logger.LogInformation("处方更新成功(简化端点)，MedicalCaseId: {Id}, PrescriptionId: {PrescriptionId}",
                    id, prescriptionId);
                return Ok(ApiResponse<PrescriptionDto>.CreateSuccess(dto, "处方更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "处方更新失败：验证失败");
                return StatusCode(403, ApiResponse<PrescriptionDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新处方", new { id, request });
            }
        }

        /// <summary>
        /// 将处方实体映射为DTO
        /// </summary>
        private static PrescriptionDto MapToPrescriptionDto(Prescription entity, Guid medicalCaseId)
        {
            return new PrescriptionDto
            {
                Id = entity.Id,
                MedicalCaseId = medicalCaseId,
                PatientId = entity.PatientId ?? Guid.Empty,
                UserId = entity.UserId ?? Guid.Empty,
                PrescriptionNumber = entity.PrescriptionNumber,
                Indication = entity.Indication,
                DosageCount = entity.DosageCount,
                Discount = entity.Discount,
                Advice = entity.Advice,
                FormulaSource = entity.FormulaSource,
                ReferencedFormulas = entity.ReferencedFormulas,
                Remark = entity.Remark,
                Items = entity.Items?.Select(item => new PrescriptionItemDto
                {
                    Id = item.Id,
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Amount, // Amount是计算属性，映射到Subtotal
                    Usage = item.Usage,
                    Remark = item.Remark
                }).ToList() ?? new List<PrescriptionItemDto>(),
                SingleDosePrice = entity.Items?.Sum(x => x.Amount) ?? 0,
                TotalPrice = (entity.Items?.Sum(x => x.Amount) ?? 0) * entity.DosageCount * entity.Discount,
                TotalWeight = entity.Items?.Sum(x => x.Quantity) ?? 0,
                Status = CommonStatus.Enabled, // 子实体状态由聚合根MedicalCase控制
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        /// <summary>
        /// 将医案实体映射为DTO
        /// </summary>
        private static MedicalCaseDto MapToMedicalCaseDto(MedicalCase entity)
        {
            return new MedicalCaseDto
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
                CreatedAt = entity.CreatedAt,
                // Issue #2231: 添加ConsultationId字段（共享主键，值等于MedicalCase.Id）
                ConsultationId = entity.Id
            };
        }

        #endregion

        #region 聚合保存端点

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <param name="request">聚合保存请求</param>
        /// <returns>更新后的病案详情</returns>
        [HttpPut("{id}/aggregate")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> SaveAggregate(
            Guid id,
            [FromBody] MedicalCaseAggregateInputDto request)
        {
            try
            {
                // 验证请求ID与路由ID一致
                if (request.Id != id)
                {
                    return BadRequest(ApiResponse<MedicalCaseDetailDto>.CreateFail("请求ID与路由ID不一致"));
                }

                // 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权编辑病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse<MedicalCaseDetailDto>.CreateFail("无权编辑此病案"));
                }

                // 获取当前用户信息
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                // 调用聚合保存服务
                var result = await _commandService.SaveAggregateAsync(request, operatorId, isAdmin);

                if (result == null)
                {
                    return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("病案不存在"));
                }

                // Entity → MedicalCaseDetailDto 映射
                var detailDto = MapToMedicalCaseDetailDto(result);

                _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
                return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(detailDto, "保存成功"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "聚合保存失败：权限不足");
                return StatusCode(403, ApiResponse<MedicalCaseDetailDto>.CreateFail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "聚合保存失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseDetailDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "保存医案聚合", new { id, request });
            }
        }

        /// <summary>
        /// 将医案实体映射为详情DTO（包含Consultation和Prescription）
        /// </summary>
        private static MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity)
        {
            return new MedicalCaseDetailDto
            {
                // 基础字段
                Id = entity.Id,
                PatientId = entity.PatientId,
                PatientName = entity.PatientName,
                DoctorId = entity.DoctorId,
                DoctorName = entity.DoctorName,
                ConsultationDate = entity.ConsultationDate,
                CaseStatus = entity.CaseStatus,
                Remark = entity.Remark,
                Diagnosis = entity.Consultation?.TCMDiagnosis,
                CreatedAt = entity.CreatedAt,

                // 详细字段
                ChiefComplaint = entity.Consultation?.ChiefComplaint,
                PresentIllness = entity.Consultation?.PresentIllness,
                DiagnosisResult = entity.Consultation?.TCMDiagnosis,
                TreatmentPlan = entity.Consultation?.TreatmentPrinciple,

                // Consultation
                Consultation = entity.Consultation != null ? new ConsultationDto
                {
                    Id = entity.Consultation.Id,
                    MedicalCaseId = entity.Id,
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
                    Remark = entity.Consultation.Remark,
                    CreatedAt = entity.Consultation.CreatedAt,
                    UpdatedAt = entity.Consultation.UpdatedAt
                } : null,

                // Prescription
                Prescription = entity.Prescription != null && !entity.Prescription.IsDeleted ? new PrescriptionDto
                {
                    Id = entity.Prescription.Id,
                    MedicalCaseId = entity.Id,
                    PatientId = entity.PatientId,
                    UserId = entity.DoctorId,
                    PrescriptionNumber = entity.Prescription.PrescriptionNumber,
                    Indication = entity.Prescription.Indication,
                    DosageCount = entity.Prescription.DosageCount,
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
                        Dosage = item.Quantity,
                        TotalPrice = item.Amount,
                        TotalWeight = item.Quantity,
                        Subtotal = item.Amount,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList() ?? new List<PrescriptionItemDto>(),
                    SingleDosePrice = entity.Prescription.Items?.Sum(x => x.Amount) ?? 0,
                    TotalPrice = (entity.Prescription.Items?.Sum(x => x.Amount) ?? 0) * entity.Prescription.DosageCount * entity.Prescription.Discount,
                    TotalWeight = entity.Prescription.Items?.Sum(x => x.Quantity) ?? 0,
                    Status = CommonStatus.Enabled,
                    CreatedAt = entity.Prescription.CreatedAt,
                    UpdatedAt = entity.Prescription.UpdatedAt
                } : null
            };
        }

        #endregion

        /// <summary>
        /// 更新病案状态
        /// Epic #1612修正版: 支持Draft/Active/Completed/Cancelled状态流转
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var result = await _stateService.UpdateStatusAsync(id, request.Status);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));

                // Entity → DTO映射
                var dto = MapToMedicalCaseDto(result);

                _logger.LogInformation("病案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}",
                    id, request.Status);
                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "状态更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                // 状态转换不合法
                _logger.LogWarning(ex, "状态更新失败：状态转换不合法");
                return UnprocessableEntity(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新病案状态", new { id, request });
            }
        }

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612 - BF-002: 三步流程验证
        /// </summary>
        /// <summary>
        /// 完成病案 - 已废弃
        /// </summary>
        /// <remarks>
        /// OpenSpec refactor-webapi-layer: 此端点从未被Client调用，
        /// Client使用 PUT /{id}/status 并指定 Completed 状态。
        /// </remarks>
        

        /// <summary>
        /// 删除病案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// refactor-authorization-system: AUTHZ-002 使用IAuthorizationService进行资源级授权
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult> DeleteMedicalCase(Guid id)
        {
            try
            {
                // refactor-authorization-system: 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Delete);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权删除病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse.CreateFail("无权删除此病案"));
                }

                var result = await _commandService.DeleteAsync(id);

                _logger.LogInformation("病案已软删除，MedicalCaseId: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                HandleException(ex, "删除病案", new { id });
                throw;
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
        public async Task<IActionResult> CloseMedicalCase(Guid id)
        {
            try
            {
                var result = await _stateService.CloseCaseAsync(id);

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

        /// <summary>
        /// 暂存医案（保存草稿）
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
        /// 保存当前数据，设置状态为Draft，不触发完成验证
        /// refactor-authorization-system: AUTHZ-002 使用IAuthorizationService进行资源级授权
        /// </summary>
        [HttpPut("{id}/draft")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 403)]
        public async Task<IActionResult> SaveDraft(
            Guid id,
            [FromBody] ConsultationInputDto? request = null)
        {
            try
            {
                // refactor-authorization-system: 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权编辑病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse<MedicalCaseDto>.CreateFail("无权编辑此病案"));
                }

                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                var result = await _stateService.SaveDraftAsync(id, request, operatorId, isAdmin);
                if (result == null)
                {
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));
                }

                // Entity → DTO映射
                var dto = MapToMedicalCaseDto(result);

                _logger.LogInformation("病案暂存成功，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "病案已暂存"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "暂存病案失败：权限不足");
                return StatusCode(403, ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "暂存病案失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "暂存病案", new { id });
            }
        }

        /// <summary>
        /// 取消医案
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
        /// 设置状态为Cancelled，需要审计理由（非当天本人操作时）
        /// refactor-authorization-system: AUTHZ-002 使用IAuthorizationService进行资源级授权
        /// </summary>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 403)]
        public async Task<IActionResult> CancelMedicalCase(
            Guid id,
            [FromBody] CancelMedicalCaseRequest? request = null)
        {
            try
            {
                // refactor-authorization-system: 资源级授权检查
                var medicalCase = await _queryService.GetByIdAsync(id);
                if (medicalCase == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));

                var authResult = await _authorizationService.AuthorizeAsync(User, medicalCase, MedicalCaseOperations.Edit);
                if (!authResult.Succeeded)
                {
                    _logger.LogWarning("授权失败: 用户无权编辑病案 {MedicalCaseId}", id);
                    return StatusCode(403, ApiResponse<MedicalCaseDto>.CreateFail("无权编辑此病案"));
                }

                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

                var result = await _stateService.CancelAsync(id, operatorId, isAdmin, request?.Reason);
                if (result == null)
                {
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));
                }

                // Entity → DTO映射
                var dto = MapToMedicalCaseDto(result);

                _logger.LogInformation("病案取消成功，MedicalCaseId: {Id}", id);
                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "病案已取消"));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "取消病案失败：权限不足");
                return StatusCode(403, ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "取消病案失败：业务规则验证失败");
                return UnprocessableEntity(ApiResponse<MedicalCaseDto>.CreateFail(ex.Message));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消病案", new { id });
            }
        }

        // ========== Read Layer（读操作，独立查询）==========

        /// <summary>
        /// 获取病案详情
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _queryService.GetByIdAsync(id);

                if (result == null)
                    return NotFound(ApiResponse<MedicalCaseDto>.CreateFail("病案不存在"));

                // Entity → DTO映射
                var dto = MapToMedicalCaseDto(result);

                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dto, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取病案详情", new { id });
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
        public async Task<IActionResult> GetMedicalCaseByIdWithDetails(Guid id)
        {
            try
            {
                var entity = await _queryService.GetByIdAsync(id);

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
                        Remark = entity.Consultation.Remark,
                        // DD-002: 移除Status字段，Consultation状态从聚合根MedicalCase派生
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
                        Status = CommonStatus.Enabled, // 子实体状态由聚合根MedicalCase控制
                        CreatedAt = entity.Prescription.CreatedAt,
                        UpdatedAt = entity.Prescription.UpdatedAt
                    } : null
                };

                return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(detailDto, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取病案详情（含关联数据）", new { id });
            }
        }

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// 返回用户是否可编辑、可删除、是否需要提供修改原因等权限信息
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>权限详情</returns>
        [HttpGet("{id}/permissions")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 404)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            try
            {
                var entity = await _queryService.GetByIdAsync(id);

                if (entity == null)
                    return NotFound(ApiResponse<MedicalCasePermissionDto>.CreateFail("病案不存在"));

                // 获取当前用户信息
                var (userId, _, role) = GetOperator();

                // 获取权限详情
                var permissions = _permissionService.GetPermissions(userId, role, entity);

                _logger.LogDebug("权限查询: 用户 {UserId}({Role}) 对医案 {MedicalCaseId} 的权限: CanEdit={CanEdit}, CanDelete={CanDelete}",
                    userId, role, id, permissions.CanEdit, permissions.CanDelete);

                return Ok(ApiResponse<MedicalCasePermissionDto>.CreateSuccess(permissions, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取病案权限", new { id });
            }
        }

        /// <summary>
        /// 获取医案的审计日志列表（分页）
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// 返回医案的所有修改历史记录
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="page">页码（默认1）</param>
        /// <param name="pageSize">每页大小（默认20）</param>
        /// <returns>审计日志分页结果</returns>
        [HttpGet("{id}/audit-logs")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 404)]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // 验证医案是否存在
                var entity = await _queryService.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail("病案不存在"));

                // 参数验证
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail(
                        "页码和页大小参数无效（页码>0，页大小1-100）"));
                }

                // 获取审计日志
                var (logs, totalCount) = await _auditService.GetLogsPagedAsync(id, page, pageSize);

                // Entity → DTO 映射
                var logDtos = logs.Select(log => new MedicalCaseAuditLogDto
                {
                    Id = log.Id,
                    MedicalCaseId = log.MedicalCaseId,
                    OperatorId = log.OperatorId,
                    OperatorName = log.OperatorName,
                    OperatorRole = log.OperatorRole,
                    OperationType = log.OperationType,
                    ChangedFields = log.ChangedFields,
                    OldValues = log.OldValues,
                    NewValues = log.NewValues,
                    Reason = log.Reason,
                    CreatedAt = log.CreatedAt
                }).ToList();

                var result = new MedicalCaseAuditLogPagedResultDto
                {
                    Logs = logDtos,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("审计日志查询: 医案 {MedicalCaseId}, 第 {Page} 页, 共 {TotalCount} 条",
                    id, page, totalCount);

                return Ok(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取审计日志", new { id, page, pageSize });
            }
        }

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// OpenSpec: optimize-module-list-ui - 添加角色过滤，Doctor只能看到自己的医案
        /// OpenSpec: fix-history-copy-all-patients - 添加includeAllDoctors参数支持历史医案复制
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeAllDoctors = false)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest(ApiResponse<PagedResult<MedicalCaseDto>>.CreateFail(
                        "页码和页大小参数无效（页码>0，页大小1-100）"));
                }

                // OpenSpec: optimize-module-list-ui - 获取当前用户信息用于角色过滤
                // OpenSpec: fix-history-copy-all-patients - includeAllDoctors=true时跳过医生过滤
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;

                var entityResult = await _queryService.GetListAsync(
                    status, patientId, page, pageSize,
                    currentDoctorId: operatorId,
                    isAdmin: isAdmin);

                // Entity → DTO映射
                // OpenSpec: fix-history-copy-all-patients - 添加ConsultationId/PrescriptionId映射
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
                    CreatedAt = entity.CreatedAt,
                    // 设置ID字段，计算属性HasConsultation/HasPrescription会自动计算
                    ConsultationId = entity.Consultation != null ? entity.Id : null,
                    PrescriptionId = (entity.Prescription != null && !entity.Prescription.IsDeleted) ? entity.Prescription.Id : null
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
                return HandleException(ex, "获取病案列表",
                    new { status, patientId, page, pageSize });
            }
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        [HttpGet("{medicalCaseId}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
        public async Task<IActionResult> GetConsultationList(
            Guid medicalCaseId)
        {
            try
            {
                var result = await _queryService.GetConsultationListAsync(medicalCaseId);

                return Ok(ApiResponse<List<ConsultationDto>>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取辨证记录列表",
                    new { medicalCaseId });
            }
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        [HttpGet("{medicalCaseId}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDto>>), 200)]
        public async Task<IActionResult> GetPrescriptionList(
            Guid medicalCaseId)
        {
            try
            {
                var result = await _queryService.GetPrescriptionListAsync(medicalCaseId);

                return Ok(ApiResponse<List<PrescriptionDto>>.CreateSuccess(result, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取处方列表",
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
        public async Task<IActionResult> GetPendingCases()
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
                        result = await _queryService.GetAllPendingCasesAsync();
                    }
                    else if (operatorRole == UserRole.Doctor)
                    {
                        // 医生只查询自己的待诊医案
                        _logger.LogInformation("医生查询自己的待诊队列，DoctorId: {DoctorId}",
                            operatorId);
                        result = await _queryService.GetPendingCasesAsync(operatorId);
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
                return HandleException(ex, "获取待诊队列", null);
            }
        }

        /// <summary>
        /// 根据患者ID获取医案列表
        /// OpenSpec: redesign-history-copy-ui - 补充缺失的API端点
        /// 复用GetListAsync方法，仅包装为专用路由
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>患者的所有医案列表</returns>
        [HttpGet("by-patient/{patientId}")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseDto>>), 200)]
        public async Task<IActionResult> GetMedicalCasesByPatientId(Guid patientId)
        {
            try
            {
                // 复用GetListAsync，设置patientId筛选，不分页（取大量数据）
                var entityResult = await _queryService.GetListAsync(
                    status: null,
                    patientId: patientId,
                    page: 1,
                    pageSize: 1000, // 取全部历史医案
                    currentDoctorId: null,
                    isAdmin: true); // 历史查询不限制医生

                // Entity → DTO映射
                // HasConsultation/HasPrescription是计算属性，只需设置ConsultationId/PrescriptionId
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
                    Diagnosis = entity.Consultation?.TCMDiagnosis,
                    CreatedAt = entity.CreatedAt,
                    // 设置ID字段，计算属性HasConsultation/HasPrescription会自动计算
                    ConsultationId = entity.Consultation != null ? entity.Id : null,
                    PrescriptionId = (entity.Prescription != null && !entity.Prescription.IsDeleted) ? entity.Prescription.Id : null
                }).ToList();

                _logger.LogInformation("根据患者ID查询医案列表，PatientId: {PatientId}, Count: {Count}",
                    patientId, dtoItems.Count);

                return Ok(ApiResponse<List<MedicalCaseDto>>.CreateSuccess(dtoItems, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "根据患者ID获取医案列表", new { patientId });
            }
        }

        [HttpGet("patient/{patientId}/unfinished")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDto>), 403)]
        public async Task<IActionResult> GetUnfinishedCaseByPatientId(
            Guid patientId,
            [FromQuery] Guid? doctorId = null,
            [FromQuery] bool checkAllDoctors = false)
        {
            try
            {
                // Epic #2210 Task 3.1.3: Q4医生筛选链 - 提取当前医生ID
                // OpenSpec: multi-doctor-unfinished-case - 支持查询所有医生的未完成医案
                Guid currentDoctorId;
                try
                {
                    var (operatorId, operatorName, operatorRole) = GetOperator();

                    // 如果checkAllDoctors=true，查询所有医生的未完成医案（用于多医生场景检测）
                    if (checkAllDoctors)
                    {
                        _logger.LogInformation("查询所有医生的未完成医案，PatientId: {PatientId}", patientId);
                        currentDoctorId = Guid.Empty; // Repository会跳过医生ID过滤
                    }
                    // 如果未传递doctorId，使用当前登录医生ID
                    else if (doctorId == null || doctorId == Guid.Empty)
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

                var entityResult = await _queryService.GetUnfinishedCaseByPatientIdAsync(patientId, currentDoctorId);

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
                    CreatedAt = entityResult.CreatedAt
                };

                return Ok(ApiResponse<MedicalCaseDto>.CreateSuccess(dtoResult, "查询成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取患者未完成医案",
                    new { patientId, doctorId });
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

    // SetPrescriptionFlagRequest 已移至 LYBT.Shared.Models.Contracts.MedicalCase

    /// <summary>
    /// 更新病案状态请求
    /// Epic #1612修正版
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>目标状态：Draft/Active/Completed/Cancelled</summary>
        public MedicalCaseStatus Status { get; set; }
    }

    /// <summary>
    /// 取消病案请求
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
    /// </summary>
    public class CancelMedicalCaseRequest
    {
        /// <summary>取消原因（非当天本人操作时必填）</summary>
        public string? Reason { get; set; }
    }
}
