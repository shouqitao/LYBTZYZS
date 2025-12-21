using AutoMapper;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
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
    /// </summary>
    public class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMedicalCaseAuditService _auditService;

        public MedicalCaseCommandService(
            IMedicalCaseRepository repository,
            IPatientRepository patientRepository,
            IUserRepository userRepository,
            IMedicalCaseAuditService auditService,
            IMapper mapper,
            ILogger<MedicalCaseCommandService> logger)
            : base(logger, mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        /// <summary>
        /// 创建新病案
        /// Epic #1612: 自动创建Consultation子实体（共享主键）
        /// Issue #2211: 修复P0 Bug - 添加doctorId参数并设置DoctorId/DoctorName/PatientName
        /// </summary>
        public async Task<MedicalCase?> CreateAsync(Guid patientId, DateTime visitDate, Guid doctorId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("开始创建病案，PatientId: {PatientId}, VisitDate: {VisitDate}, DoctorId: {DoctorId}",
                patientId, visitDate, doctorId);

            // 参数验证：doctorId不能为Guid.Empty
            if (doctorId == Guid.Empty)
            {
                _logger.LogWarning("DoctorId不能为空Guid");
                throw new ArgumentException("DoctorId不能为空", nameof(doctorId));
            }

            // 查询Patient获取PatientName
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning("患者不存在，PatientId: {PatientId}", patientId);
                throw new InvalidOperationException($"患者不存在，PatientId: {patientId}");
            }

            // 查询User获取DoctorName
            var doctor = await _userRepository.GetByIdAsync(doctorId);
            if (doctor == null)
            {
                _logger.LogWarning("医生不存在，DoctorId: {DoctorId}", doctorId);
                throw new InvalidOperationException($"医生不存在，DoctorId: {doctorId}");
            }

            // 业务规则验证：BR-001（单患者仅一条未完成病案）- Epic #1731 集成Rules
            var existingActiveCases = await _repository.GetByPatientIdAsync(patientId);
            if (!MedicalCaseRules.CanCreateNewCase(existingActiveCases))
            {
                // Issue #xxxx: 区分Active和Draft状态，给出不同的错误提示
                if (MedicalCaseRules.HasActiveCase(existingActiveCases))
                {
                    var activeCase = existingActiveCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Active);
                    _logger.LogWarning("患者已有进行中的医案，PatientId: {PatientId}, ActiveCaseId: {CaseId}",
                        patientId, activeCase?.Id);
                    throw new InvalidOperationException("该患者已有进行中的医案，请先完成现有医案");
                }
                else if (MedicalCaseRules.HasDraftCase(existingActiveCases))
                {
                    var draftCase = existingActiveCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Draft);
                    _logger.LogWarning("患者已有暂存的医案，PatientId: {PatientId}, DraftCaseId: {CaseId}",
                        patientId, draftCase?.Id);
                    throw new InvalidOperationException("该患者已有暂存的医案，请先处理现有医案（继续或关闭）");
                }
            }

            // 创建MedicalCase实体
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = patient.Name,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = false, // 默认值，用户可后续修改
                UserId = doctorId,
                DoctorName = doctor.RealName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 聚合根模式：自动创建关联的Consultation（共享主键）
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            // OpenSpec: refactor-server-ddd-aggregates - 移除反向导航，仅使用共享主键关联
            var consultation = new Consultation
            {
                Id = medicalCase.Id, // 共享主键（Consultation.Id == MedicalCase.Id）
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            medicalCase.Consultation = consultation;

            // EF Core会级联保存Consultation
            var result = await _repository.AddAsync(medicalCase);

            _logger.LogInformation("病案创建成功，MedicalCaseId: {Id}, ConsultationId: {ConsultationId}, Doctor: {DoctorName}, Patient: {PatientName}",
                result.Id, consultation.Id, medicalCase.DoctorName, medicalCase.PatientName);

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 记录创建审计日志
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
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            // 确定医生ID：优先使用DTO中的UserId（非空），否则使用currentUserId
            var doctorId = request.UserId != Guid.Empty ? request.UserId : currentUserId;

            _logger.LogInformation("开始创建病案(InputDto方式)，PatientId: {PatientId}, UserId: {UserId}",
                request.PatientId, doctorId);

            // 参数验证：doctorId不能为Guid.Empty
            if (doctorId == Guid.Empty)
            {
                _logger.LogWarning("UserId不能为空Guid");
                throw new ArgumentException("UserId不能为空", nameof(request));
            }

            // 查询Patient获取PatientName
            var patient = await _patientRepository.GetByIdAsync(request.PatientId);
            if (patient == null)
            {
                _logger.LogWarning("患者不存在，PatientId: {PatientId}", request.PatientId);
                throw new InvalidOperationException($"患者不存在，PatientId: {request.PatientId}");
            }

            // 查询User获取DoctorName
            var doctor = await _userRepository.GetByIdAsync(doctorId);
            if (doctor == null)
            {
                _logger.LogWarning("医生不存在，UserId: {UserId}", doctorId);
                throw new InvalidOperationException($"医生不存在，UserId: {doctorId}");
            }

            // 业务规则验证：BR-001（单患者仅一条未完成病案）
            var existingActiveCases = await _repository.GetByPatientIdAsync(request.PatientId);
            if (!MedicalCaseRules.CanCreateNewCase(existingActiveCases))
            {
                if (MedicalCaseRules.HasActiveCase(existingActiveCases))
                {
                    var activeCase = existingActiveCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Active);
                    _logger.LogWarning("患者已有进行中的医案，PatientId: {PatientId}, ActiveCaseId: {CaseId}",
                        request.PatientId, activeCase?.Id);
                    throw new InvalidOperationException("该患者已有进行中的医案，请先完成现有医案");
                }
                else if (MedicalCaseRules.HasDraftCase(existingActiveCases))
                {
                    var draftCase = existingActiveCases.FirstOrDefault(c => c.CaseStatus == MedicalCaseStatus.Draft);
                    _logger.LogWarning("患者已有暂存的医案，PatientId: {PatientId}, DraftCaseId: {CaseId}",
                        request.PatientId, draftCase?.Id);
                    throw new InvalidOperationException("该患者已有暂存的医案，请先处理现有医案（继续或关闭）");
                }
            }

            // 创建MedicalCase实体
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                PatientName = patient.Name,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = request.Prescription?.NeedsPrescription ?? false,
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
                consultation.TCMDiagnosis = request.Consultation.TCMDiagnosis;
            }

            medicalCase.Consultation = consultation;

            // 如果DTO中提供了处方数据且需要开处方，创建Prescription
            if (request.Prescription != null && request.Prescription.NeedsPrescription)
            {
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = medicalCase.Id,
                    DosageCount = request.Prescription.DosageCount,
                    Usage = request.Prescription.Usage,
                    Advice = request.Prescription.Advice,
                    ReferencedFormulas = request.Prescription.ReferencedFormulas,
                    Discount = request.Prescription.Discount,
                    // TotalPrice由Items计算，不直接存储
                    Remark = request.Prescription.Remark,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>()
                };

                // 添加处方项
                if (request.Prescription.Items != null)
                {
                    foreach (var itemDto in request.Prescription.Items)
                    {
                        var item = new LYBT.Entities.Prescriptions.PrescriptionItem
                        {
                            Id = Guid.NewGuid(),
                            PrescriptionId = prescription.Id,
                            HerbId = itemDto.HerbId,
                            HerbName = itemDto.HerbName ?? string.Empty,
                            Dosage = itemDto.Dosage,
                            Unit = itemDto.Unit,
                            UnitPrice = itemDto.UnitPrice,
                            Usage = request.Prescription.Usage,
                            Remark = itemDto.Remark,
                            DecocteMethod = itemDto.DecocteMethod
                        };
                        prescription.Items.Add(item);
                    }
                }

                medicalCase.Prescription = prescription;
                _logger.LogInformation("已创建处方，PrescriptionId: {PrescriptionId}, Items: {ItemCount}",
                    prescription.Id, prescription.Items.Count);
            }

            // EF Core会级联保存Consultation和Prescription
            var result = await _repository.AddAsync(medicalCase);

            _logger.LogInformation("病案创建成功(InputDto方式)，MedicalCaseId: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                result.Id, medicalCase.DoctorName, medicalCase.PatientName);

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
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("开始更新辨证信息，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // 获取聚合根（完整加载）
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return null;
            }

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // Epic #1731: 权限检查 - 集成CanEdit规则（包含管理员权限和状态验证）
            // MedicalCaseRules.CanEdit已完整处理：
            // - 管理员可以编辑所有状态的医案
            // - 医生只能编辑自己的Draft/Active状态医案
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            if (!MedicalCaseRules.CanEdit(medicalCase, currentUserId, isAdmin))
            {
                var reason = isAdmin ? "权限不足" :
                    (medicalCase.UserId != currentUserId ? "非创建医生" :
                    $"医案状态为{medicalCase.CaseStatus}");
                _logger.LogWarning("无权限编辑病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}, Reason: {Reason}",
                    medicalCaseId, currentUserId, reason);
                throw new UnauthorizedAccessException($"无权限编辑此病案：{reason}");
            }

            // 确保Consultation存在
            if (medicalCase.Consultation == null)
            {
                _logger.LogWarning("Consultation不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw new InvalidOperationException("病案的辨证信息不存在");
            }

            // Issue #2231: 手动映射属性以避免EF Core共享主键冲突
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            var consultation = medicalCase.Consultation;
            consultation.PresentIllness = request.PresentIllness;
            consultation.TongueDiagnosis = request.TongueDiagnosis;
            consultation.PulseDiagnosis = request.PulseDiagnosis;
            consultation.TCMDiagnosis = request.TCMDiagnosis;
            consultation.UpdatedAt = DateTime.Now;

            // 通过聚合根保存（EF Core会跟踪子实体变更）
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 记录更新审计日志
            var operatorInfo = await GetOperatorInfoAsync(currentUserId, isAdmin);
            await _auditService.LogAsync(
                before: beforeState,
                after: result,
                operatorId: currentUserId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Update);

            return result;
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612: 动态流程控制，用户可选择跳过处方
        /// </summary>
        public async Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("设置处方标志，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                medicalCaseId, needsPrescription);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return null;
            }

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 保存变更前的状态用于审计
            var beforeState = CloneMedicalCaseForAudit(medicalCase);

            // Epic #1731: 权限检查 - 集成CanEdit规则
            if (!MedicalCaseRules.CanEdit(medicalCase, currentUserId, isAdmin))
            {
                _logger.LogWarning("无权限编辑病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                    medicalCaseId, currentUserId);
                throw new UnauthorizedAccessException("无权限编辑此病案");
            }

            // 更新NeedsPrescription标志
            // OpenSpec: consultation-field-alignment - 处方标志统一在MedicalCase管理
            medicalCase.NeedsPrescription = needsPrescription;
            medicalCase.UpdatedAt = DateTime.Now;

            // 保存
            var result = await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("处方标志设置成功，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                medicalCaseId, needsPrescription);

            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 记录更新审计日志
            var operatorInfo = await GetOperatorInfoAsync(currentUserId, isAdmin);
            await _auditService.LogAsync(
                before: beforeState,
                after: result,
                operatorId: currentUserId,
                operatorName: operatorInfo.Name,
                role: operatorInfo.Role,
                operationType: AuditOperationType.Update);

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
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("开始创建处方，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}", medicalCaseId, attempt);

                    // 获取聚合根（使用Fresh版本确保获取最新RowVersion，解决并发问题）
                    var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
                    if (medicalCase == null)
                    {
                        _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                        return null;
                    }

                    // 业务规则验证：验证处方需求标记
                    if (medicalCase.NeedsPrescription != true)
                    {
                        _logger.LogWarning("未标记需要开处方，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                            medicalCaseId, medicalCase.NeedsPrescription);
                        throw new InvalidOperationException("未标记需要开处方，请先设置处方需求标记");
                    }

                    // 业务规则验证：AR-003（一诊一方约束）
                    if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
                    {
                        _logger.LogWarning("病案已存在处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                            medicalCaseId, medicalCase.Prescription.Id);
                        throw new InvalidOperationException($"病案已存在处方（ID: {medicalCase.Prescription.Id}），请使用更新接口");
                    }

                    // 创建Prescription实体（不包含Items，需手动处理）
                    // OpenSpec: optimize-entity-data-flow - PatientId/UserId通过MedicalCase获取
                    var prescription = _mapper.Map<Prescription>(request);
                    prescription.Id = Guid.NewGuid();
                    prescription.MedicalCaseId = medicalCaseId;
                    prescription.CreatedAt = DateTime.Now;
                    prescription.UpdatedAt = DateTime.Now;

                    // 手动处理Items（确保PrescriptionId正确设置）
                    if (request.Items != null && request.Items.Any())
                    {
                        prescription.Items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>();
                        foreach (var itemDto in request.Items)
                        {
                            var item = _mapper.Map<LYBT.Entities.Prescriptions.PrescriptionItem>(itemDto);
                            item.Id = Guid.NewGuid();
                            item.PrescriptionId = prescription.Id;
                            prescription.Items.Add(item);
                        }
                        _logger.LogInformation("处方项创建完成，共{Count}项", request.Items.Count);
                    }

                    // 关联到聚合根
                    medicalCase.Prescription = prescription;
                    medicalCase.UpdatedAt = DateTime.Now;

                    // 通过聚合根保存（EF Core会级联创建Prescription和Items）
                    await _repository.UpdateAsync(medicalCase);

                    _logger.LogInformation("处方创建成功，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                        medicalCaseId, prescription.Id);

                    return prescription;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex) when (attempt < maxRetries)
                {
                    // 并发冲突（EF Core原生异常），重试
                    _logger.LogWarning(ex, "创建处方遇到EF并发冲突，准备重试，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}",
                        medicalCaseId, attempt);
                    await Task.Delay(100 * attempt); // 递增延迟
                    continue;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("数据已被其他用户修改") && attempt < maxRetries)
                {
                    // 并发冲突（Repository层封装的异常），重试
                    _logger.LogWarning("创建处方遇到并发冲突，准备重试，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}",
                        medicalCaseId, attempt);
                    await Task.Delay(100 * attempt); // 递增延迟
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建处方失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw;
                }
            }

            _logger.LogError("创建处方失败，已达最大重试次数，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            throw new InvalidOperationException("创建处方失败，请稍后重试");
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
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("开始更新处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                medicalCaseId, prescriptionId);

            // 获取聚合根（使用Fresh版本确保获取最新RowVersion，解决并发问题）
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return null;
            }

            // Epic #1731: 权限检查 - 集成CanEdit规则
            if (!MedicalCaseRules.CanEdit(medicalCase, currentUserId, isAdmin))
            {
                _logger.LogWarning("无权限编辑病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                    medicalCaseId, currentUserId);
                throw new UnauthorizedAccessException("无权限编辑此病案");
            }

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("处方不存在或ID不匹配，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                    medicalCaseId, prescriptionId);
                return null;
            }

            // 业务规则验证：已打印处方不允许修改
            if (medicalCase.Prescription.IsPrinted)
            {
                _logger.LogWarning("处方已打印，不允许修改，PrescriptionId: {PrescriptionId}", prescriptionId);
                throw new InvalidOperationException("处方已打印，不允许修改");
            }

            // 通过AutoMapper更新Prescription子实体（不包含Items）
            _mapper.Map(request, medicalCase.Prescription);
            medicalCase.Prescription.UpdatedAt = DateTime.Now;
            medicalCase.UpdatedAt = DateTime.Now;

            // 手动处理Items更新（AutoMapper无法正确处理集合更新）
            if (request.Items != null)
            {
                // 清除旧的处方项
                medicalCase.Prescription.Items.Clear();

                // 添加新的处方项
                foreach (var itemDto in request.Items)
                {
                    var item = _mapper.Map<LYBT.Entities.Prescriptions.PrescriptionItem>(itemDto);
                    item.Id = Guid.NewGuid();
                    item.PrescriptionId = prescriptionId;
                    medicalCase.Prescription.Items.Add(item);
                }

                _logger.LogInformation("处方项更新完成，共{Count}项", request.Items.Count);
            }

            // 通过聚合根保存
            await _repository.UpdateAsync(medicalCase);

            _logger.LogInformation("处方更新成功，PrescriptionId: {PrescriptionId}", prescriptionId);
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
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("开始删除处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                medicalCaseId, prescriptionId);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return false;
            }

            // Epic #1731: 权限检查 - 集成CanDelete规则
            if (!MedicalCaseRules.CanDelete(medicalCase, currentUserId, isAdmin))
            {
                _logger.LogWarning("无权限删除病案处方，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}",
                    medicalCaseId, currentUserId);
                throw new UnauthorizedAccessException("无权限删除此病案的处方");
            }

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("处方不存在或ID不匹配，PrescriptionId: {PrescriptionId}", prescriptionId);
                return false;
            }

            // 业务规则验证：已打印处方不允许删除
            if (medicalCase.Prescription.IsPrinted)
            {
                _logger.LogWarning("处方已打印，不允许删除，PrescriptionId: {PrescriptionId}", prescriptionId);
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

            _logger.LogInformation("处方删除成功，PrescriptionId: {PrescriptionId}", prescriptionId);
            return true;
        }

        /// <summary>
        /// 删除病案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("开始软删除病案，MedicalCaseId: {MedicalCaseId}", id);

            // 使用Repository的软删除（BaseRepository.DeleteAsync设置IsDeleted=true）
            var result = await _repository.DeleteAsync(id);

            if (result)
            {
                _logger.LogInformation("病案软删除成功，MedicalCaseId: {MedicalCaseId}", id);
            }
            else
            {
                _logger.LogWarning("病案软删除失败（不存在），MedicalCaseId: {MedicalCaseId}", id);
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
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await ExecuteSaveAttemptAsync(request, medicalCaseId, currentUserId, isAdmin, attempt);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "保存医案聚合根遇到EF并发冲突，准备重试，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}",
                        medicalCaseId, attempt);
                    await Task.Delay(100 * attempt);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("数据已被其他用户修改") && attempt < maxRetries)
                {
                    _logger.LogWarning("保存医案聚合根遇到并发冲突，准备重试，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}",
                        medicalCaseId, attempt);
                    await Task.Delay(100 * attempt);
                }
            }

            _logger.LogError("保存医案聚合根失败，已达最大重试次数，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            throw new InvalidOperationException("保存失败，请稍后重试");
        }

        /// <summary>
        /// 执行单次保存尝试
        /// consolidate-code-quality: 从SaveAsync提取核心逻辑
        /// </summary>
        private async Task<MedicalCase> ExecuteSaveAttemptAsync(
            MedicalCaseInputDto request,
            Guid medicalCaseId,
            Guid currentUserId,
            bool isAdmin,
            int attempt)
        {
            _logger.LogInformation("开始保存医案聚合根，MedicalCaseId: {MedicalCaseId}, 尝试次数: {Attempt}",
                medicalCaseId, attempt);

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
            _logger.LogInformation("医案聚合根保存成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            await LogUpdateAuditAsync(beforeState, result, currentUserId, isAdmin);
            return result;
        }

        /// <summary>
        /// 验证编辑权限
        /// </summary>
        private void ValidateEditPermission(MedicalCase medicalCase, Guid currentUserId, bool isAdmin)
        {
            if (MedicalCaseRules.CanEdit(medicalCase, currentUserId, isAdmin)) return;

            var reason = isAdmin ? "权限不足" :
                (medicalCase.UserId != currentUserId ? "非创建医生" : $"医案状态为{medicalCase.CaseStatus}");

            _logger.LogWarning("无权限编辑病案，MedicalCaseId: {MedicalCaseId}, UserId: {UserId}, Reason: {Reason}",
                medicalCase.Id, currentUserId, reason);
            throw new UnauthorizedAccessException($"无权限编辑此病案：{reason}");
        }

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
        private void UpdateConsultationFields(Consultation consultation, ConsultationInputDto dto)
        {
            consultation.PresentIllness = dto.PresentIllness;
            consultation.TongueDiagnosis = dto.TongueDiagnosis;
            consultation.PulseDiagnosis = dto.PulseDiagnosis;
            consultation.TCMDiagnosis = dto.TCMDiagnosis;
            consultation.UpdatedAt = DateTime.Now;

            // 日志由调用方ExecuteSaveAttemptAsync统一记录
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
                _logger.LogInformation("已软删除处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
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
                Items = CreatePrescriptionItems(Guid.NewGuid(), prescriptionDto)
            };
            prescription.Items = CreatePrescriptionItems(prescription.Id, prescriptionDto);

            medicalCase.Prescription = prescription;
            _logger.LogInformation("已创建处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}, Items: {ItemCount}",
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

            _logger.LogInformation("已更新处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}, Items: {ItemCount}",
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

        /// <summary>
        /// 克隆医案实体用于审计比较
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId, ConsultationDate移除
        private static MedicalCase CloneMedicalCaseForAudit(MedicalCase source)
        {
            return new MedicalCase
            {
                Id = source.Id,
                PatientId = source.PatientId,
                PatientName = source.PatientName,
                UserId = source.UserId,
                DoctorName = source.DoctorName,
                CaseStatus = source.CaseStatus,
                Remark = source.Remark,
                NeedsPrescription = source.NeedsPrescription,
                IsDeleted = source.IsDeleted,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        /// <summary>
        /// 获取操作者信息用于审计日志
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        private async Task<(string Name, UserRole Role)> GetOperatorInfoAsync(Guid userId, bool isAdmin)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    return (user.RealName, user.Role);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取操作者信息失败，UserId: {UserId}", userId);
            }

            // 回退到基本信息
            return (
                "Unknown",
                isAdmin ? UserRole.Admin : UserRole.Doctor
            );
        }

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
                    _logger.LogInformation("批量删除 - 医案已删除: {MedicalCaseId}", id);
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
                    _logger.LogError(ex, "批量删除 - 删除医案失败: {MedicalCaseId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return LYBT.Shared.Models.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>.Success(result);
        }
    }
}
