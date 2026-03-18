using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using EC = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案命令服务实现 - 写操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：Create, Update, Delete操作
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用MedicalCaseMapper替代AutoMapper
    /// </summary>
    public class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IPatientCrossModuleService _patientCrossModule;
        private readonly IUserCrossModuleService _userCrossModule;
        private readonly IHerbCrossModuleService _herbCrossModule;
        private readonly IMedicalCaseAuditService _auditService;
        private readonly IMedicalCasePermissionService _permissionService;
        private readonly MedicalCaseMapper _mapper = new();
        private readonly ICacheInvalidationService _cacheInvalidation;

        public MedicalCaseCommandService(
            IMedicalCaseRepository repository,
            IRegistrationRepository registrationRepository,
            IPatientCrossModuleService patientCrossModule,
            IUserCrossModuleService userCrossModule,
            IHerbCrossModuleService herbCrossModule,
            IMedicalCaseAuditService auditService,
            IMedicalCasePermissionService permissionService,
            ILogger<MedicalCaseCommandService> logger,
            ICacheInvalidationService cacheInvalidation)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _registrationRepository = registrationRepository ?? throw new ArgumentNullException(nameof(registrationRepository));
            _patientCrossModule = patientCrossModule ?? throw new ArgumentNullException(nameof(patientCrossModule));
            _userCrossModule = userCrossModule ?? throw new ArgumentNullException(nameof(userCrossModule));
            _herbCrossModule = herbCrossModule ?? throw new ArgumentNullException(nameof(herbCrossModule));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
        }

        /// <summary>
        /// 创建新医案 (委托给 CreateFromInputDtoAsync)
        /// </summary>
        public async Task<MedicalCase?> CreateAsync(Guid patientId, DateTime visitDate, Guid doctorId)
        {
            var request = new MedicalCaseInputDto
            {
                PatientId = patientId,
                UserId = doctorId
            };

            return await CreateFromInputDtoAsync(request, doctorId);
        }

        /// <summary>
        /// 从InputDto创建医案（统一SaveAsync的创建分支）
        /// OpenSpec: simplify-medicalcase-dataflow Phase 2 - 统一创建/更新
        /// </summary>
        /// <param name="request">统一输入DTO</param>
        /// <param name="currentUserId">当前操作用户ID（如果DTO未提供UserId则使用此值）</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>创建的医案实体</returns>
        private async Task<MedicalCase?> CreateFromInputDtoAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false)
        {
            var doctorId = request.UserId != Guid.Empty ? request.UserId : currentUserId;

            _logger.LogInformation("[SVC] MedicalCase.CreateFromInput started - PatientId={PatientId} UserId={UserId}",
                request.PatientId, doctorId);

            // 统一验证: 参数、Patient、Doctor、BR-001
            var (patient, doctor) = await MedicalCaseServiceHelper.ValidateAndFetchCreationContextAsync(
                request.PatientId, doctorId, _patientCrossModule, _userCrossModule, _repository, _logger);

            // 创建MedicalCase实体
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                CaseNumber = await GenerateCaseNumberAsync(),  // T5-P2-11: 自动生成医案编号
                PatientId = request.PatientId,
                PatientName = patient.Name,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = request.Prescription?.NeedsPrescription,
                UserId = doctorId,
                DoctorName = doctor.RealName,
                Remark = request.Remark,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 创建Consultation（聚合根模式：共享主键）
            // OpenSpec: refactor-server-ddd-aggregates - 移除反向导航，仅使用共享主键关联
            var consultation = new Consultation
            {
                Id = medicalCase.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 如果DTO中提供了诊断数据，填充Consultation字段
            if (request.Consultation != null)
            {
                // T5-P2-10: 创建时也验证TcmDiagnosis非空
                // 注: UpdateConsultationAsync 通过 FluentValidation Pipeline 验证，
                // 但 CreateFromInputDtoAsync 的 Consultation 数据是直接映射，不经过验证器
                if (string.IsNullOrWhiteSpace(request.Consultation.TcmDiagnosis))
                {
                    _logger.LogInformation("[SVC] MedicalCase.Create -> TcmDiagnosisEmpty");
                    throw new BusinessException(EC.MedicalCaseMissingDiagnosis, "中医诊断不能为空");
                }

                consultation.PresentIllness = request.Consultation.PresentIllness;
                consultation.TongueDiagnosis = request.Consultation.TongueDiagnosis;
                consultation.PulseDiagnosis = request.Consultation.PulseDiagnosis;
                consultation.TcmDiagnosis = request.Consultation.TcmDiagnosis;
            }

            medicalCase.Consultation = consultation;

            // 如果DTO中提供了处方数据且需要开处方，创建Prescription
            if (request.Prescription != null && request.Prescription.NeedsPrescription)
            {
                await CreateNewPrescriptionAsync(medicalCase, request.Prescription);
            }

            var result = await _repository.AddAsync(medicalCase);

            // 如果传入了 RegistrationId，更新关联挂号的 MedicalCaseId
            if (request.RegistrationId.HasValue)
            {
                var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId.Value);
                if (registration != null)
                {
                    registration.MedicalCaseId = result.Id;
                    await _registrationRepository.UpdateAsync(registration);
                    _logger.LogInformation("[SVC] MedicalCase.CreateFromInput -> Registration linked - RegistrationId={RegistrationId}, MedicalCaseId={MedicalCaseId}",
                        registration.Id, result.Id);
                }
                else
                {
                    _logger.LogWarning("[SVC] MedicalCase.CreateFromInput -> Registration not found - RegistrationId={RegistrationId}",
                        request.RegistrationId.Value);
                }
            }

            await _cacheInvalidation.InvalidateAsync("medicalcases");

            // 记录创建审计日志
            await _auditService.LogAsync(
                before: null,
                after: result,
                operatorId: doctorId,
                operatorName: doctor.RealName,
                role: doctor.Role,
                operationType: AuditOperationType.Create);

            return result;
        }

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// Epic #1612: 通过聚合根协调Consultation更新
        /// 业务规则：AR-001（聚合根约束）、BF-002（三步流程）
        /// </summary>
        public async Task<MedicalCase?> UpdateConsultationAsync(
            Guid medicalCaseId,
            ConsultationInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            string? editReason = null)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // 获取聚合根（完整加载）
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogInformation("医案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return null;
            }

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "UpdateConsultation", _logger);

            // S3: 需要修改原因时，验证 editReason 不为空
            if (_permissionService.RequiresEditReason(medicalCase, currentUserId) && string.IsNullOrWhiteSpace(editReason))
            {
                throw new BusinessException(EC.McPrintedRequiresReason, "该医案需要提供修改原因");
            }

            // 确保Consultation存在
            if (medicalCase.Consultation == null)
            {
                _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation → ConsultationNotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                throw new BusinessException(EC.McConsultationNotFound, "医案的辨证信息不存在");
            }

            // Issue #2231: 手动映射属性以避免EF Core共享主键冲突
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            var consultation = medicalCase.Consultation;
            consultation.PresentIllness = request.PresentIllness;
            consultation.TongueDiagnosis = request.TongueDiagnosis;
            consultation.PulseDiagnosis = request.PulseDiagnosis;
            consultation.TcmDiagnosis = request.TcmDiagnosis;
            consultation.UpdatedAt = DateTime.UtcNow;

            // CODE-02: 编辑已打印医案时重置 IsPrinted（防御性编程）
            if (medicalCase.IsPrinted)
            {
                medicalCase.IsPrinted = false;
                medicalCase.PrintVersion++;
                _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation -> ResetIsPrinted - MedicalCaseId={Id}", medicalCase.Id);
            }

            // 通过聚合根保存（EF Core会跟踪子实体变更）
            var result = await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");
            // S3-03: 传递 editReason 到审计日志
            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin, editReason);
            return result;
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// </summary>
        public async Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag - MedicalCaseId={MedicalCaseId} NeedsPrescription={NeedsPrescription}",
                medicalCaseId, needsPrescription);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.SetPrescriptionFlag → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "SetPrescriptionFlag", _logger);

            // 更新NeedsPrescription标志
            // OpenSpec: consultation-field-alignment - 处方标志统一在MedicalCase管理
            medicalCase.NeedsPrescription = needsPrescription;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // T5-P2-12: 标记不需要处方时，软删除已有处方
            if (!needsPrescription)
            {
                SoftDeletePrescriptionIfExists(medicalCase);
            }

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");
            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin);
            return result;
        }

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612: 通过聚合根创建Prescription
        /// 业务规则：AR-001（聚合根约束）、AR-003（一诊一方约束）
        /// </summary>
        public async Task<Prescription?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionInputDto request)
        {
            return await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
                () => ExecuteCreatePrescriptionAsync(medicalCaseId, request),
                "CreatePrescription", _logger);
        }

        /// <summary>
        /// 执行单次处方创建
        /// </summary>
        private async Task<Prescription?> ExecuteCreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionInputDto request)
        {
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.CreatePrescription -> NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            if (medicalCase.NeedsPrescription != true)
                throw new BusinessException(EC.McPrescriptionFlagNotSet, "未标记需要开处方，请先设置处方需求标记");

            if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
                throw new BusinessException(EC.McPrescriptionAlreadyExists, $"医案已存在处方（ID: {medicalCase.Prescription.Id}），请使用更新接口");

            var prescription = _mapper.ToPrescriptionEntity(request);
            prescription.Id = Guid.NewGuid();
            prescription.PrescriptionNumber = await GeneratePrescriptionNumberAsync();  // T5-P2-13
            prescription.MedicalCaseId = medicalCaseId;
            prescription.CreatedAt = DateTime.UtcNow;
            prescription.UpdatedAt = DateTime.UtcNow;

            // T2-S4-02: 使用统一的CreatePrescriptionItemsAsync确保UnitPrice自动填充
            prescription.Items = await CreatePrescriptionItemsAsync(prescription.Id, request);

            medicalCase.Prescription = prescription;
            medicalCase.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");

            _logger.LogInformation("[SVC] MedicalCase.CreatePrescription completed - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescription.Id);

            return prescription;
        }

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// Epic #1612: 通过聚合根更新Prescription
        /// </summary>
        public async Task<Prescription?> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            PrescriptionInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            string? editReason = null)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdatePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescriptionId);

            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // S3-03: 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "UpdatePrescription", _logger);

            // S3: 需要修改原因时，验证 editReason 不为空
            if (_permissionService.RequiresEditReason(medicalCase, currentUserId) && string.IsNullOrWhiteSpace(editReason))
            {
                throw new BusinessException(EC.McPrintedRequiresReason, "该医案需要提供修改原因");
            }

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → PrescriptionNotFound - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCaseId, prescriptionId);
                return null;
            }

            // T2-X8-01: 打印保护 -- 已打印的完成态医案禁止修改处方
            if (medicalCase.IsPrinted && medicalCase.IsCompleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → PrintProtected - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                throw new BusinessException(EC.McPrintedCannotDelete, "医案已打印并完成，不允许修改处方");
            }

            // 通过Mapperly更新Prescription子实体（不包含Items）
            _mapper.UpdatePrescriptionEntity(request, medicalCase.Prescription);
            medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // T2-S4-02: 使用统一的CreatePrescriptionItemsAsync确保UnitPrice自动填充
            if (request.Items != null)
            {
                medicalCase.Prescription.Items.Clear();
                foreach (var item in await CreatePrescriptionItemsAsync(prescriptionId, request))
                {
                    medicalCase.Prescription.Items.Add(item);
                }
            }

            // CODE-02: 编辑已打印医案时重置 IsPrinted（防御性编程）
            if (medicalCase.IsPrinted)
            {
                medicalCase.IsPrinted = false;
                medicalCase.PrintVersion++;
                _logger.LogInformation("[SVC] MedicalCase.UpdatePrescription -> ResetIsPrinted - MedicalCaseId={Id}", medicalCase.Id);
            }

            await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");

            // S3-03: 记录处方更新审计日志 (含 editReason)
            await LogUpdateAuditAsync(beforeState, medicalCase, currentUserId, isAdmin, editReason);

            return medicalCase.Prescription;
        }

        /// <summary>
        /// 删除处方（软删除）
        /// Epic #1612: 通过聚合根删除Prescription
        /// 业务规则：仅允许删除未打印处方
        /// </summary>
        public async Task<bool> DeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            Guid currentUserId,
            bool isAdmin = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.DeletePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescriptionId);

            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return false;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanDelete(_permissionService, medicalCase, currentUserId, isAdmin, "DeletePrescription", _logger);

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → PrescriptionNotFound - PrescriptionId={PrescriptionId}", prescriptionId);
                return false;
            }

            // T2-X8-01: 打印保护 -- 已打印的完成态医案禁止删除处方
            if (medicalCase.IsPrinted && medicalCase.IsCompleted)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → PrintProtected - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                throw new BusinessException(EC.McPrintedCannotDelete, "医案已打印并完成，不允许删除处方");
            }

            // 软删除Prescription
            medicalCase.Prescription.IsDeleted = true;
            medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;

            // 清空导航属性（保持聚合根一致性）
            medicalCase.Prescription = null;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // 通过聚合根保存
            await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");
            return true;
        }

        /// <summary>
        /// 删除医案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin)
        {
            _logger.LogInformation("[SVC] MedicalCase.Delete - MedicalCaseId={MedicalCaseId} OperatorId={OperatorId}", id, operatorId);

            var medicalCase = await _repository.GetByIdAsync(id);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.Delete -> NotFound - MedicalCaseId={MedicalCaseId}", id);
                return false;
            }

            // 权限检查: 确保操作者有权删除此医案
            MedicalCaseServiceHelper.EnsureCanDelete(_permissionService, medicalCase, operatorId, isAdmin, "Delete", _logger);

            var result = await _repository.DeleteAsync(id);
            if (result)
            {
                await _cacheInvalidation.InvalidateAsync("medicalcases");
            }
            return result;
        }

        /// <summary>
        /// 统一保存医案（支持创建和更新）
        /// OpenSpec: simplify-medicalcase-dataflow Phase 2 - 统一SaveAsync
        /// - Id为null时：创建新MedicalCase
        /// - Id有值时：更新现有MedicalCase
        /// - 在单个事务中同时保存诊断和处方数据
        /// </summary>
        public async Task<MedicalCase?> SaveAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false)
        {
            // OpenSpec: simplify-medicalcase-dataflow - 统一创建/更新逻辑
            if (!request.Id.HasValue)
            {
                return await CreateFromInputDtoAsync(request, currentUserId, isAdmin);
            }

            var medicalCaseId = request.Id.Value;
            return await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
                () => ExecuteSaveAttemptAsync(request, medicalCaseId, currentUserId, isAdmin),
                "Save", _logger);
        }

        /// <summary>
        /// 执行单次保存尝试
        /// consolidate-code-quality: 从SaveAsync提取核心逻辑
        /// </summary>
        private async Task<MedicalCase> ExecuteSaveAttemptAsync(
            MedicalCaseInputDto request,
            Guid medicalCaseId,
            Guid currentUserId,
            bool isAdmin)
        {

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId)
                ?? throw ExceptionFactory.MedicalCase.NotFound(medicalCaseId);

            // 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            ValidateEditPermission(medicalCase, currentUserId, isAdmin);

            // S3-03: 需要修改原因时，验证 editReason 不为空
            if (_permissionService.RequiresEditReason(medicalCase, currentUserId) && string.IsNullOrWhiteSpace(request.EditReason))
            {
                throw new BusinessException(EC.McPrintedRequiresReason, "该医案需要提供修改原因");
            }

            // 更新基础字段
            UpdateMedicalCaseBasicFields(medicalCase, request);

            // 更新诊断
            if (request.Consultation != null && medicalCase.Consultation != null)
            {
                UpdateConsultationFields(medicalCase.Consultation, request.Consultation);
            }

            // 更新处方
            if (request.Prescription != null)
            {
                // T2-X8-01: 打印保护 -- 已打印的完成态医案禁止修改处方
                if (medicalCase.IsPrinted && medicalCase.IsCompleted)
                {
                    _logger.LogWarning("[SVC] MedicalCase.Save → PrintProtected - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                    throw new BusinessException(EC.McPrintedCannotDelete, "医案已打印并完成，不允许修改处方");
                }

                await HandlePrescriptionUpdateAsync(medicalCase, request.Prescription);
            }

            // CODE-02: 编辑已打印医案时重置 IsPrinted
            if (medicalCase.IsPrinted)
            {
                medicalCase.IsPrinted = false;
                medicalCase.PrintVersion++;
                _logger.LogInformation("[SVC] MedicalCase.Save -> ResetIsPrinted - MedicalCaseId={Id}", medicalCase.Id);
            }

            // 保存并审计
            var result = await _repository.UpdateAsync(medicalCase);
            await _cacheInvalidation.InvalidateAsync("medicalcases");
            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // S3-03: 传递 editReason 到审计日志
            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin, request.EditReason);
            return result;
        }

        /// <summary>
        /// 验证编辑权限 (委托给 ServiceHelper)
        /// </summary>
        private void ValidateEditPermission(MedicalCase medicalCase, Guid currentUserId, bool isAdmin)
            => MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "Save", _logger);

        /// <summary>
        /// 更新医案基础字段
        /// </summary>
        private static void UpdateMedicalCaseBasicFields(MedicalCase medicalCase, MedicalCaseInputDto request)
        {
            if (!string.IsNullOrEmpty(request.Remark))
            {
                medicalCase.Remark = request.Remark;
            }
            medicalCase.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新诊断字段
        /// </summary>
        private static void UpdateConsultationFields(Consultation consultation, ConsultationInputDto dto)
        {
            consultation.PresentIllness = dto.PresentIllness;
            consultation.TongueDiagnosis = dto.TongueDiagnosis;
            consultation.PulseDiagnosis = dto.PulseDiagnosis;
            consultation.TcmDiagnosis = dto.TcmDiagnosis;
            consultation.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 记录更新审计日志
        /// S3-03: 新增 editReason 参数，传递到审计日志
        /// </summary>
        private async Task LogUpdateAuditAsync(MedicalCase before, MedicalCase after, Guid currentUserId, bool isAdmin, string? editReason = null)
        {
            var operatorInfo = await GetOperatorInfoAsync(currentUserId, isAdmin);
            await _auditService.LogAsync(
                before: before,
                after: after,
                operatorId: currentUserId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Update,
                reason: editReason);
        }


        /// <summary>
        /// 处理处方更新(创建/更新/软删除)
        /// consolidate-code-quality: 从SaveAsync提取，降低圈复杂度
        /// </summary>
        private async Task HandlePrescriptionUpdateAsync(
            MedicalCase medicalCase,
            PrescriptionInputDto prescriptionDto)
        {
            medicalCase.NeedsPrescription = prescriptionDto.NeedsPrescription;

            if (!prescriptionDto.NeedsPrescription)
            {
                SoftDeletePrescriptionIfExists(medicalCase);
                return;
            }

            if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
            {
                await CreateNewPrescriptionAsync(medicalCase, prescriptionDto);
            }
            else
            {
                await UpdateExistingPrescriptionAsync(medicalCase.Prescription, prescriptionDto);
            }
        }

        /// <summary>
        /// 软删除现有处方
        /// </summary>
        private void SoftDeletePrescriptionIfExists(MedicalCase medicalCase)
        {
            if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
            {
                medicalCase.Prescription.IsDeleted = true;
                medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionSoftDeleted - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCase.Id, medicalCase.Prescription.Id);
            }
        }

        /// <summary>
        /// 创建新处方
        /// </summary>
        private async Task CreateNewPrescriptionAsync(
            MedicalCase medicalCase,
            PrescriptionInputDto prescriptionDto)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PrescriptionNumber = await GeneratePrescriptionNumberAsync(),  // T5-P2-13: 自动生成处方编号
                MedicalCaseId = medicalCase.Id,
                DosageCount = prescriptionDto.DosageCount,
                Usage = prescriptionDto.Usage,
                Advice = prescriptionDto.Advice,
                ReferencedFormulas = prescriptionDto.ReferencedFormulas,
                Discount = prescriptionDto.Discount,
                Remark = prescriptionDto.Remark,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>()
            };
            prescription.Items = await CreatePrescriptionItemsAsync(prescription.Id, prescriptionDto);

            medicalCase.Prescription = prescription;
            _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionCreated - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId} ItemCount={ItemCount}",
                medicalCase.Id, prescription.Id, prescription.Items.Count);
        }

        /// <summary>
        /// 更新现有处方
        /// </summary>
        private async Task UpdateExistingPrescriptionAsync(
            Prescription prescription,
            PrescriptionInputDto prescriptionDto)
        {
            prescription.DosageCount = prescriptionDto.DosageCount;
            prescription.Usage = prescriptionDto.Usage;
            prescription.Advice = prescriptionDto.Advice;
            prescription.ReferencedFormulas = prescriptionDto.ReferencedFormulas;
            prescription.Discount = prescriptionDto.Discount;
            prescription.Remark = prescriptionDto.Remark;
            prescription.UpdatedAt = DateTime.UtcNow;

            prescription.Items.Clear();
            foreach (var item in await CreatePrescriptionItemsAsync(prescription.Id, prescriptionDto))
            {
                prescription.Items.Add(item);
            }

            _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionUpdated - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId} ItemCount={ItemCount}",
                prescription.MedicalCaseId, prescription.Id, prescription.Items.Count);
        }

        /// <summary>
        /// 创建处方项列表（含UnitPrice自动填充）
        /// </summary>
        /// <remarks>
        /// T2-S4-02: 当客户端未传UnitPrice（值为0）时，从药材库自动查询当前价格填充。
        /// 防御性设计，确保TotalPrice计算正确。
        /// </remarks>
        private async Task<List<LYBT.Entities.Prescriptions.PrescriptionItem>> CreatePrescriptionItemsAsync(
            Guid prescriptionId,
            PrescriptionInputDto prescriptionDto)
        {
            var items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>();

            if (prescriptionDto.Items == null || !prescriptionDto.Items.Any()) return items;

            var allHerbIds = prescriptionDto.Items.Select(i => i.HerbId).Distinct().ToList();

            // AD-02: 过滤禁用药材，禁止加入处方
            var disabledHerbIds = await _herbCrossModule.GetDisabledHerbIdsAsync(allHerbIds);
            var validItems = prescriptionDto.Items;
            if (disabledHerbIds.Count > 0)
            {
                var skippedNames = prescriptionDto.Items
                    .Where(i => disabledHerbIds.Contains(i.HerbId))
                    .Select(i => i.HerbName ?? i.HerbId.ToString())
                    .Distinct();
                _logger.LogWarning("[SVC] AD-02: Skipped {Count} disabled herbs from prescription: {HerbNames}",
                    disabledHerbIds.Count, string.Join(", ", skippedNames));

                validItems = prescriptionDto.Items
                    .Where(i => !disabledHerbIds.Contains(i.HerbId))
                    .ToList();
            }

            // T2-S4-02: 批量查询缺失UnitPrice的药材价格
            var herbIdsNeedingPrice = validItems
                .Where(i => i.UnitPrice <= 0)
                .Select(i => i.HerbId)
                .Distinct()
                .ToList();

            Dictionary<Guid, decimal>? herbPrices = null;
            if (herbIdsNeedingPrice.Count > 0)
            {
                herbPrices = await _herbCrossModule.GetHerbPricesAsync(herbIdsNeedingPrice);
                _logger.LogInformation("[SVC] Auto-populated UnitPrice for {Count} herbs from herb catalog",
                    herbPrices.Count);
            }

            foreach (var itemDto in validItems)
            {
                var unitPrice = itemDto.UnitPrice;
                if (unitPrice <= 0 && herbPrices != null && herbPrices.TryGetValue(itemDto.HerbId, out var herbPrice))
                {
                    unitPrice = herbPrice;
                }

                items.Add(new LYBT.Entities.Prescriptions.PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescriptionId,
                    HerbId = itemDto.HerbId,
                    HerbName = itemDto.HerbName ?? string.Empty,
                    Dosage = itemDto.Dosage,
                    Unit = itemDto.Unit,
                    UnitPrice = unitPrice,
                    Usage = itemDto.Usage,
                    Remark = itemDto.Remark,
                    DecocteMethod = itemDto.DecocteMethod
                });
            }

            return items;
        }

        #region Private Helper Methods

        private static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
            => MedicalCaseServiceHelper.CloneMedicalCaseForAudit(source);

        private async Task<(string Name, UserRole Role)> GetOperatorInfoAsync(Guid userId, bool isAdmin)
            => await MedicalCaseServiceHelper.GetOperatorInfoAsync(_userCrossModule, userId, isAdmin, _logger);

        /// <summary>
        /// 生成医案编号（格式：MC + 年月日 + 序号）
        /// T5-P2-11: 参考 LocalMedicalCaseDataSource.GenerateCaseNumber
        /// </summary>
        private async Task<string> GenerateCaseNumberAsync()
        {
            var today = DateTime.Today;
            var dateStr = today.ToString("yyyyMMdd");
            var prefix = $"MC{dateStr}";

            // 查询今天的医案数量（包含软删除的，避免编号重复）
            var count = await _repository.CountByPrefixAsync(prefix);
            return $"{prefix}{(count + 1):D3}";
        }

        /// <summary>
        /// 生成处方编号（格式：RX + 年月日 + 序号）
        /// T5-P2-13
        /// </summary>
        private async Task<string> GeneratePrescriptionNumberAsync()
        {
            var today = DateTime.Today;
            var dateStr = today.ToString("yyyyMMdd");
            var prefix = $"RX{dateStr}";

            var count = await _repository.CountPrescriptionsByPrefixAsync(prefix);
            return $"{prefix}{(count + 1):D4}";
        }

        #endregion

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc />
        public async Task<LYBT.Shared.Models.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin)
        {
            var result = new LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new LYBT.Shared.Models.Contracts.Common.BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "医案不存在"
                        });
                        continue;
                    }

                    // 权限检查: 确保操作者有权删除此医案
                    MedicalCaseServiceHelper.EnsureCanDelete(_permissionService, entity, operatorId, isAdmin, "BatchDelete", _logger);

                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] MedicalCase.BatchDelete → ItemSuccess - MedicalCaseId={MedicalCaseId}", id);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new LYBT.Shared.Models.Contracts.Common.BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "[SVC] MedicalCase.BatchDelete → ItemFailed - MedicalCaseId={MedicalCaseId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return LYBT.Shared.Models.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>.Success(result);
        }
    }
}
