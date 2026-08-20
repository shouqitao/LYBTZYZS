using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using FluentValidation;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案命令服务实现 - 写操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：Create, Update, Delete操作
    /// </summary>
    public partial class MedicalCaseCommandService : BaseService<MedicalCase>, IMedicalCaseCommandService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IRegistrationCrossModuleService _registrationCrossModule;
        private readonly IPatientCrossModuleService _patientCrossModule;
        private readonly IUserCrossModuleService _userCrossModule;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly MedicalCasePrescriptionService _prescriptionService;
        private readonly PrescriptionItemService _itemService;
        private readonly MedicalCaseMapper _mapper;
        private readonly IValidator<MedicalCaseInputDto> _inputValidator;

        public MedicalCaseCommandService(
            IMedicalCaseRepository repository,
            IRegistrationCrossModuleService registrationCrossModule,
            IPatientCrossModuleService patientCrossModule,
            IUserCrossModuleService userCrossModule,
            ILogger<MedicalCaseCommandService> logger,
            ICacheInvalidationService cacheInvalidation,
            MedicalCasePrescriptionService prescriptionService,
            PrescriptionItemService itemService,
            MedicalCaseMapper mapper,
            IValidator<MedicalCaseInputDto> inputValidator)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _registrationCrossModule = registrationCrossModule ?? throw new ArgumentNullException(nameof(registrationCrossModule));
            _patientCrossModule = patientCrossModule ?? throw new ArgumentNullException(nameof(patientCrossModule));
            _userCrossModule = userCrossModule ?? throw new ArgumentNullException(nameof(userCrossModule));
            _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
            _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _mapper = mapper;
            _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        }

        /// <summary>
        /// 从InputDto创建医案（统一SaveAsync的创建分支）
        /// </summary>
        /// <param name="request">统一输入DTO</param>
        /// <param name="currentUserId">当前操作用户ID（如果DTO未提供UserId则使用此值）</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的医案实体</returns>
        private async Task<MedicalCase?> CreateFromInputDtoAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            // P0-2: 回填 UserId（客户端未传时使用当前用户），与下方 doctorId 兜底逻辑一致
            if (request.UserId == Guid.Empty)
                request.UserId = currentUserId;
            var doctorId = request.UserId;

            // P0-2: 统一验证（PatientId/UserId 必填 + 处方嵌套规则）
            await _inputValidator.ValidateAndThrowAsync(request, cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.CreateFromInput started - PatientId={PatientId} UserId={UserId}",
                request.PatientId, doctorId);

            // 统一验证: 参数、Patient、Doctor、BR-001
            var (patient, doctor) = await MedicalCaseServiceHelper.ValidateAndFetchCreationContextAsync(
                request.PatientId, doctorId, _patientCrossModule, _userCrossModule, _repository, _logger, cancellationToken);

            // 创建MedicalCase实体
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                CaseNumber = await GenerateCaseNumberAsync(cancellationToken),  // T5-P2-11: 自动生成医案编号
                PatientId = request.PatientId,
                PatientName = patient.Name,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = request.Prescription?.NeedsPrescription,
                UserId = doctorId,
                DoctorName = doctor.RealName,
                // 2026-08-13（startvisit-createdby-fix）: CreatedBy 必填（DB NOT NULL——真机 start-visit 500 根因）
                // 创建者 = 当前操作医生（跨模块创建——StartVisit 接诊即建路径）
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 创建Consultation（聚合根模式：共享主键）
            // 2026-08-13（consultation-createdby-fix）: CreatedBy 必填（DB NOT NULL——真机 start-visit 500 根因，
            // 与 MedicalCase.CreatedBy 同源（startvisit-createdby-fix）——CreatedBy = 当前操作医生）
            var consultation = new Consultation
            {
                Id = medicalCase.Id,
                CreatedBy = currentUserId,
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
                    throw new BusinessException(ErrorCode.MedicalCaseMissingDiagnosis, "中医诊断不能为空");
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
                await _itemService.CreateNewPrescriptionAsync(medicalCase, request.Prescription, currentUserId, cancellationToken);
            }

            var result = await _repository.AddAsync(medicalCase, cancellationToken);

            // 如果传入了 RegistrationId，更新关联挂号的 MedicalCaseId
            if (request.RegistrationId.HasValue)
            {
                await _registrationCrossModule.LinkRegistrationToMedicalCaseAsync(
                    request.RegistrationId.Value, result.Id, cancellationToken);
                _logger.LogInformation("[SVC] MedicalCase.CreateFromInput -> Registration linked - RegistrationId={RegistrationId}, MedicalCaseId={MedicalCaseId}",
                    request.RegistrationId.Value, result.Id);
            }

            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            return result;
        }

        /// <inheritdoc />
        public Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
            => _prescriptionService.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription, currentUserId, isAdmin, cancellationToken);

        /// <summary>
        /// 统一保存医案（支持创建和更新）
        /// - Id为null时：创建新MedicalCase
        /// - Id有值时：更新现有MedicalCase
        /// - 在单个事务中同时保存诊断和处方数据
        /// </summary>
        public async Task<MedicalCase?> SaveAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            if (!request.Id.HasValue)
            {
                return await CreateFromInputDtoAsync(request, currentUserId, isAdmin, cancellationToken);
            }

            var medicalCaseId = request.Id.Value;
            return await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
                () => ExecuteSaveAttemptAsync(request, medicalCaseId, currentUserId, isAdmin, cancellationToken),
                "Save", _logger);
        }

        /// <summary>
        /// 执行单次保存尝试
        /// consolidate-code-quality: 从SaveAsync提取核心逻辑
        /// </summary>
        private async Task<MedicalCase?> ExecuteSaveAttemptAsync(
            MedicalCaseInputDto request,
            Guid medicalCaseId,
            Guid currentUserId,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
                return null;

            // 权限检查
            ValidateEditPermission(medicalCase, currentUserId, isAdmin);

            // P0-4 (R3 C2): EditReason 校验——Completed/IsLocked/IsPrinted/异人编辑均需原因（去 &&!isAdmin，补 IsLocked/异人）
            ValidateEditReason(medicalCase, request, currentUserId);

            // T5-3 #16 (US-MC-017): 更新前快照（审计字段 diff）
            var before = CaptureSnapshot(medicalCase);

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
                await _itemService.HandlePrescriptionUpdateAsync(medicalCase, request.Prescription, currentUserId);
            }

            // 打印保护简化（2026-08-03）：打印后修改内容 → IsPrinted=false、PrintVersion++（提示重新打印）
            MedicalCaseServiceHelper.ResetPrintMarker(medicalCase);

            // 保存
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            // T5-3 #16 (US-MC-017): 更新审计（含字段 diff）——原仅取消操作写审计
            await WriteUpdateAuditAsync(medicalCase, request, currentUserId, before, cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }

        /// <summary>
        /// P0-4 (R3 C2): EditReason 校验——Completed/IsLocked/IsPrinted/异人编辑均需原因（去 &&!isAdmin，补 IsLocked/异人）
        /// </summary>
        private static void ValidateEditReason(MedicalCase medicalCase, MedicalCaseInputDto request, Guid currentUserId)
        {
            var isPrintedEdit = medicalCase.IsPrinted && medicalCase.PrintVersion > 0;
            var isCompletedEdit = medicalCase.CaseStatus == MedicalCaseStatus.Completed;
            var isLockedEdit = medicalCase.IsLocked;
            var isForeignEdit = medicalCase.UserId != currentUserId;
            if ((isPrintedEdit || isCompletedEdit || isLockedEdit || isForeignEdit) && string.IsNullOrWhiteSpace(request.EditReason))
            {
                throw new InvalidOperationException(
                    isPrintedEdit
                        ? "医案已打印，修改内容需提供编辑原因"
                        : isLockedEdit
                            ? "医案已锁定（隔天），编辑需提供编辑原因"
                            : isForeignEdit
                                ? "非创建医生编辑需提供编辑原因"
                                : "已完成医案编辑需提供编辑原因");
            }
        }

        /// <summary>
        /// T5-3 #16: 更新前快照（审计字段 diff 对比基准）
        /// </summary>
        private static Dictionary<string, string?> CaptureSnapshot(MedicalCase medicalCase)
            => new()
            {
                ["PresentIllness"] = medicalCase.Consultation?.PresentIllness,
                ["TongueDiagnosis"] = medicalCase.Consultation?.TongueDiagnosis,
                ["PulseDiagnosis"] = medicalCase.Consultation?.PulseDiagnosis,
                ["TcmDiagnosis"] = medicalCase.Consultation?.TcmDiagnosis,
                ["PrescriptionItems"] = medicalCase.Prescription?.Items?.Count.ToString() ?? "0"
            };

        /// <summary>
        /// T5-3 #16: 保存后写更新审计（ChangedFields/OldValues/NewValues 填充——原 20 字段 diff 未实现）
        /// </summary>
        private async Task WriteUpdateAuditAsync(
            MedicalCase medicalCase,
            MedicalCaseInputDto request,
            Guid currentUserId,
            Dictionary<string, string?> before,
            CancellationToken cancellationToken)
        {
            var changed = new Dictionary<string, (string? Old, string? New)>();
            void Compare(string field, string? oldVal, string? newVal)
            {
                if (!string.Equals(oldVal, newVal))
                    changed[field] = (oldVal, newVal);
            }

            Compare("PresentIllness", before["PresentIllness"], medicalCase.Consultation?.PresentIllness);
            Compare("TongueDiagnosis", before["TongueDiagnosis"], medicalCase.Consultation?.TongueDiagnosis);
            Compare("PulseDiagnosis", before["PulseDiagnosis"], medicalCase.Consultation?.PulseDiagnosis);
            Compare("TcmDiagnosis", before["TcmDiagnosis"], medicalCase.Consultation?.TcmDiagnosis);
            Compare("PrescriptionItems", before["PrescriptionItems"], medicalCase.Prescription?.Items?.Count.ToString() ?? "0");

            if (changed.Count == 0)
                return;

            var operatorInfo = await _userCrossModule.GetUserBasicInfoAsync(currentUserId, cancellationToken);
            await _repository.AddAuditLogAsync(new MedicalCaseAuditLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCase.Id,
                // 2026-08-13（consultation-createdby-fix 同类排查）: 审计记录 CreatedBy = 操作者
                CreatedBy = currentUserId,
                OperatorId = currentUserId,
                OperatorName = operatorInfo?.UserName ?? string.Empty,
                OperatorRole = operatorInfo?.Role != null ? (int)operatorInfo.Role : 0,
                OperationType = AuditOperationUpdate,
                Reason = request.EditReason,
                ChangedFields = string.Join(",", changed.Keys),
                OldValues = System.Text.Json.JsonSerializer.Serialize(changed.ToDictionary(k => k.Key, v => v.Value.Old)),
                NewValues = System.Text.Json.JsonSerializer.Serialize(changed.ToDictionary(k => k.Key, v => v.Value.New)),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        /// <summary>审计操作类型：更新</summary>
        private const int AuditOperationUpdate = 1;

        /// <summary>
        /// 验证编辑权限 (委托给 ServiceHelper)
        /// </summary>
        private void ValidateEditPermission(MedicalCase medicalCase, Guid currentUserId, bool isAdmin)
            => MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, currentUserId, isAdmin, "Save", _logger);

        /// <summary>
        /// 更新医案基础字段
        /// </summary>
        private static void UpdateMedicalCaseBasicFields(MedicalCase medicalCase, MedicalCaseInputDto request)
        {
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
        /// 统一保存医案并返回详情DTO（含NotFound语义）
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>> SaveWithDetailAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await SaveAsync(request, currentUserId, isAdmin, cancellationToken);
            if (medicalCase == null)
                return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var dto = _mapper.MapToMedicalCaseDetailDto(medicalCase);
            return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Success(dto);
        }

        /// <summary>
        /// 标记是否需要开处方并返回详情DTO（含NotFound语义）
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>> SetPrescriptionFlagWithDetailAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await SetPrescriptionFlagAsync(medicalCaseId, needsPrescription, currentUserId, isAdmin, cancellationToken);
            if (medicalCase == null)
                return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var dto = _mapper.MapToMedicalCaseDetailDto(medicalCase);
            return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Success(dto);
        }

        /// <summary>
        /// 记录打印完成（回写打印状态 + 记录日志）
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<bool>> RecordPrintAsync(
            Guid medicalCaseId,
            int printType,
            string? printerName,
            Guid operatorId,
            string operatorName,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
                return LYBT.Shared.Models.Contracts.Common.Result<bool>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var now = DateTime.UtcNow;

            medicalCase.IsPrinted = true;
            medicalCase.PrintCount += 1;
            medicalCase.LastPrintedAt = now;
            medicalCase.PrintVersion += 1;

            await _repository.UpdateAsync(medicalCase, cancellationToken);

            var printLog = new MedicalCasePrintLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                // 2026-08-13（consultation-createdby-fix 同类排查）: 打印日志 CreatedBy = 操作者
                CreatedBy = operatorId,
                PrintType = printType,
                PrintVersion = medicalCase.PrintVersion,
                PrinterName = printerName,
                PrintedBy = operatorName,
                PrintedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddPrintLogAsync(printLog, cancellationToken);

            return LYBT.Shared.Models.Contracts.Common.Result<bool>.Success(true);
        }

        #region 私有辅助方法

        /// <summary>
        /// 生成医案编号（格式：MC + 年月日 + 序号）
        /// T5-P2-11: 参考 LocalMedicalCaseDataSource.GenerateCaseNumber
        /// </summary>
        private async Task<string> GenerateCaseNumberAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var dateStr = today.ToString("yyyyMMdd");
            var prefix = $"MC{dateStr}";

            // 查询今天的医案数量（包含软删除的，避免编号重复）
            var count = await _repository.CountByPrefixAsync(prefix, cancellationToken);
            return $"{prefix}{(count + 1):D3}";
        }

        #endregion
    }
}
