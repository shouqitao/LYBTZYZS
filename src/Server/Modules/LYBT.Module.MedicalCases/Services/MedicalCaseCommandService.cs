// P2-16-4 Row-Level 已在 MedicalCaseCommandService.EnsureCanEdit 校验 CreatedBy==currentUserId||IsAdmin，UI仅变灰，安全边界在API
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using FluentValidation;
using LYBT.Infrastructure.Caching;
using LYBT.Shared.Models.Validators.Consultation;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using LYBT.Module.MedicalCases.Guards;
using LYBT.Shared.Configuration.Options.Common;
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
        private readonly IValidator<ConsultationInputDto> _consultationValidator;
        private readonly IMedicalCaseTimeService _timeService;
        private readonly MedicalCaseStateGuard _stateGuard;
        private readonly IDomainEventDispatcher _domainEventDispatcher;

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
            IValidator<MedicalCaseInputDto> inputValidator,
            IValidator<ConsultationInputDto> consultationValidator,
            IMedicalCaseTimeService timeService,
            MedicalCaseStateGuard stateGuard,
            IDomainEventDispatcher domainEventDispatcher)
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
            _consultationValidator = consultationValidator ?? throw new ArgumentNullException(nameof(consultationValidator));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _stateGuard = stateGuard ?? throw new ArgumentNullException(nameof(stateGuard));
            _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
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

            // T1.2: 统一状态守卫入口 — IsLocked/Completed/IsPrinted/异人（via MedicalCaseStateGuard）
            _stateGuard.EnsureCanEdit(medicalCase, request.EditReason, currentUserId);

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

            // P1-19: 先追加审计（saveChanges=false，仅入 ChangeTracker）再统一 Update 单次 SaveChanges——
            // 审计与业务同事务原子提交；若 UpdateAsync 失败则审计同回滚，杜绝「审计丢失/业务已落」
            await WriteUpdateAuditAsync(medicalCase, request, currentUserId, before, cancellationToken);

            // 保存（含审计日志同次提交）
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
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
        /// design-03 §2：同一 DbContext（MedicalCaseDbContext：MedicalCases + MedicalCasePrintLogs）内的两步写
        /// 用显式事务包住——原两次独立 SaveChanges 各自成事务，日志写入失败会留下「打印计数已 +1 却无打印日志」的半写状态
        /// （P3-9 双真相以 PrintLogs 为准，半写会让打印状态与日志不符）；现两写同提交、失败同回滚。
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

            await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);
            try
            {
                await _repository.UpdateAsync(medicalCase, cancellationToken);
                await _repository.AddPrintLogAsync(printLog, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return LYBT.Shared.Models.Contracts.Common.Result<bool>.Success(true);
        }
    }
}
