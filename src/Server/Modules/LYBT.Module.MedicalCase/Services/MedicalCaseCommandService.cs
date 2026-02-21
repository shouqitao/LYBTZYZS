using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 病案命令服务实现 - 写操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：Create, Update, Delete操作
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用MedicalCaseMapper替代AutoMapper
    /// </summary>
    public class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMedicalCaseAuditService _auditService;
        private readonly IMedicalCasePermissionService _permissionService;
        private readonly MedicalCaseMapper _mapper = new();

        public MedicalCaseCommandService(
            IMedicalCaseRepository repository,
            IPatientRepository patientRepository,
            IUserRepository userRepository,
            IMedicalCaseAuditService auditService,
            IMedicalCasePermissionService permissionService,
            ILogger<MedicalCaseCommandService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        }

        /// <summary>
        /// 创建新病案 (委托给 CreateFromInputDtoAsync)
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
        /// <returns>创建的病案实体</returns>
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
                request.PatientId, doctorId, _patientRepository, _userRepository, _repository, _logger);

            // 创建MedicalCase实体
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                PatientName = patient.Name,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = request.Prescription?.NeedsPrescription,
                UserId = doctorId,
                DoctorName = doctor.RealName,
                Remark = request.Remark,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 创建Consultation（聚合根模式：共享主键）
            // OpenSpec: refactor-server-ddd-aggregates - 移除反向导航，仅使用共享主键关联
            var consultation = new Consultation
            {
                Id = medicalCase.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 如果DTO中提供了诊断数据，填充Consultation字段
            if (request.Consultation != null)
            {
                consultation.PresentIllness = request.Consultation.PresentIllness;
                consultation.TongueDiagnosis = request.Consultation.TongueDiagnosis;
                consultation.PulseDiagnosis = request.Consultation.PulseDiagnosis;
                consultation.TcmDiagnosis = request.Consultation.TcmDiagnosis;
            }

            medicalCase.Consultation = consultation;

            // 如果DTO中提供了处方数据且需要开处方，创建Prescription
            if (request.Prescription != null && request.Prescription.NeedsPrescription)
            {
                CreateNewPrescription(medicalCase, request.Prescription);
            }

            var result = await _repository.AddAsync(medicalCase);

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
            bool isAdmin = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdateConsultation - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // 获取聚合根（完整加载）
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return null;
            }

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "UpdateConsultation", _logger);

            // 确保Consultation存在
            if (medicalCase.Consultation == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdateConsultation → ConsultationNotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                throw new InvalidOperationException("病案的辨证信息不存在");
            }

            // Issue #2231: 手动映射属性以避免EF Core共享主键冲突
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            var consultation = medicalCase.Consultation;
            consultation.PresentIllness = request.PresentIllness;
            consultation.TongueDiagnosis = request.TongueDiagnosis;
            consultation.PulseDiagnosis = request.PulseDiagnosis;
            consultation.TcmDiagnosis = request.TcmDiagnosis;
            consultation.UpdatedAt = DateTime.Now;

            // 通过聚合根保存（EF Core会跟踪子实体变更）
            var result = await _repository.UpdateAsync(medicalCase);
            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin);
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
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);
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
                throw new InvalidOperationException("未标记需要开处方，请先设置处方需求标记");

            if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
                throw new InvalidOperationException($"病案已存在处方（ID: {medicalCase.Prescription.Id}），请使用更新接口");

            var prescription = _mapper.ToPrescriptionEntity(request);
            prescription.Id = Guid.NewGuid();
            prescription.MedicalCaseId = medicalCaseId;
            prescription.CreatedAt = DateTime.Now;
            prescription.UpdatedAt = DateTime.Now;

            if (request.Items != null && request.Items.Any())
            {
                prescription.Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>();
                foreach (var itemDto in request.Items)
                {
                    var item = _mapper.ToPrescriptionItemEntity(itemDto);
                    item.Id = Guid.NewGuid();
                    item.PrescriptionId = prescription.Id;
                    prescription.Items.Add(item);
                }
            }

            medicalCase.Prescription = prescription;
            medicalCase.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(medicalCase);

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
            bool isAdmin = false)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdatePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescriptionId);

            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(_permissionService, medicalCase, currentUserId, isAdmin, "UpdatePrescription", _logger);

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → PrescriptionNotFound - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCaseId, prescriptionId);
                return null;
            }

            // 业务规则验证：已打印处方不允许修改
            if (medicalCase.Prescription.IsPrinted)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → AlreadyPrinted - PrescriptionId={PrescriptionId}", prescriptionId);
                throw new InvalidOperationException("处方已打印，不允许修改");
            }

            // 通过AutoMapper更新Prescription子实体（不包含Items）
            _mapper.UpdatePrescriptionEntity(request, medicalCase.Prescription);
            medicalCase.Prescription.UpdatedAt = DateTime.Now;
            medicalCase.UpdatedAt = DateTime.Now;

            // 手动处理Items更新（AutoMapper无法正确处理集合更新）
            if (request.Items != null)
            {
                medicalCase.Prescription.Items.Clear();
                foreach (var itemDto in request.Items)
                {
                    var item = _mapper.ToPrescriptionItemEntity(itemDto);
                    item.Id = Guid.NewGuid();
                    item.PrescriptionId = prescriptionId;
                    medicalCase.Prescription.Items.Add(item);
                }
            }

            await _repository.UpdateAsync(medicalCase);
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

            // 业务规则验证：已打印处方不允许删除
            if (medicalCase.Prescription.IsPrinted)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → AlreadyPrinted - PrescriptionId={PrescriptionId}", prescriptionId);
                throw new InvalidOperationException("处方已打印，不允许删除");
            }

            // 软删除Prescription
            medicalCase.Prescription.IsDeleted = true;
            medicalCase.Prescription.UpdatedAt = DateTime.Now;

            // 清空导航属性（保持聚合根一致性）
            medicalCase.Prescription = null;
            medicalCase.UpdatedAt = DateTime.Now;

            // 通过聚合根保存
            await _repository.UpdateAsync(medicalCase);
            return true;
        }

        /// <summary>
        /// 删除病案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("[SVC] MedicalCase.Delete - MedicalCaseId={MedicalCaseId}", id);
            var result = await _repository.DeleteAsync(id);
            if (!result)
                _logger.LogWarning("[SVC] MedicalCase.Delete -> NotFound - MedicalCaseId={MedicalCaseId}", id);
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
                ?? throw new InvalidOperationException($"病案 {medicalCaseId} 不存在");

            // 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // 权限检查
            ValidateEditPermission(medicalCase, currentUserId, isAdmin);

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
                HandlePrescriptionUpdate(medicalCase, request.Prescription);
            }

            // 保存并审计
            var result = await _repository.UpdateAsync(medicalCase);
            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin);
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
            medicalCase.UpdatedAt = DateTime.Now;
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
            consultation.UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// 记录更新审计日志
        /// </summary>
        private async Task LogUpdateAuditAsync(MedicalCase before, MedicalCase after, Guid currentUserId, bool isAdmin)
        {
            var operatorInfo = await GetOperatorInfoAsync(currentUserId, isAdmin);
            await _auditService.LogAsync(
                before: before,
                after: after,
                operatorId: currentUserId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Update);
        }


        /// <summary>
        /// 处理处方更新(创建/更新/软删除)
        /// consolidate-code-quality: 从SaveAsync提取，降低圈复杂度
        /// </summary>
        private void HandlePrescriptionUpdate(
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
                CreateNewPrescription(medicalCase, prescriptionDto);
            }
            else
            {
                UpdateExistingPrescription(medicalCase.Prescription, prescriptionDto);
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
                medicalCase.Prescription.UpdatedAt = DateTime.Now;
                _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionSoftDeleted - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCase.Id, medicalCase.Prescription.Id);
            }
        }

        /// <summary>
        /// 创建新处方
        /// </summary>
        private void CreateNewPrescription(
            MedicalCase medicalCase,
            PrescriptionInputDto prescriptionDto)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCase.Id,
                DosageCount = prescriptionDto.DosageCount,
                Advice = prescriptionDto.Advice,
                ReferencedFormulas = prescriptionDto.ReferencedFormulas,
                Discount = prescriptionDto.Discount,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>()
            };
            prescription.Items = CreatePrescriptionItems(prescription.Id, prescriptionDto);

            medicalCase.Prescription = prescription;
            _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionCreated - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId} ItemCount={ItemCount}",
                medicalCase.Id, prescription.Id, prescription.Items.Count);
        }

        /// <summary>
        /// 更新现有处方
        /// </summary>
        private void UpdateExistingPrescription(
            Prescription prescription,
            PrescriptionInputDto prescriptionDto)
        {
            prescription.DosageCount = prescriptionDto.DosageCount;
            prescription.Advice = prescriptionDto.Advice;
            prescription.ReferencedFormulas = prescriptionDto.ReferencedFormulas;
            prescription.Discount = prescriptionDto.Discount;
            prescription.UpdatedAt = DateTime.Now;

            prescription.Items.Clear();
            foreach (var item in CreatePrescriptionItems(prescription.Id, prescriptionDto))
            {
                prescription.Items.Add(item);
            }

            _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionUpdated - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId} ItemCount={ItemCount}",
                prescription.MedicalCaseId, prescription.Id, prescription.Items.Count);
        }

        /// <summary>
        /// 创建处方项列表
        /// </summary>
        private static List<LYBT.Entities.Prescriptions.PrescriptionItem> CreatePrescriptionItems(
            Guid prescriptionId,
            PrescriptionInputDto prescriptionDto)
        {
            var items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>();

            if (prescriptionDto.Items == null) return items;

            foreach (var itemDto in prescriptionDto.Items)
            {
                items.Add(new LYBT.Entities.Prescriptions.PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescriptionId,
                    HerbId = itemDto.HerbId,
                    HerbName = itemDto.HerbName ?? string.Empty,
                    Dosage = itemDto.Dosage,
                    Unit = itemDto.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    Usage = prescriptionDto.Usage,
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
            => await MedicalCaseServiceHelper.GetOperatorInfoAsync(_userRepository, userId, isAdmin, _logger);

        #endregion

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc />
        public async Task<LYBT.Shared.Models.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
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

                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
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
