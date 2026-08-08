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
        private readonly ICrossModuleService _crossModule;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly MedicalCasePrescriptionService _prescriptionService;
        private readonly PrescriptionItemService _itemService;
        private readonly MedicalCaseMapper _mapper;
        private readonly IValidator<MedicalCaseInputDto> _inputValidator;

        public MedicalCaseCommandService(
            IMedicalCaseRepository repository,
            IRegistrationCrossModuleService registrationCrossModule,
            ICrossModuleService crossModule,
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
            _crossModule = crossModule ?? throw new ArgumentNullException(nameof(crossModule));
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
                request.PatientId, doctorId, _crossModule, _repository, _logger, cancellationToken);

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 创建Consultation（聚合根模式：共享主键）
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
                await _itemService.CreateNewPrescriptionAsync(medicalCase, request.Prescription, cancellationToken);
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
                await _itemService.HandlePrescriptionUpdateAsync(medicalCase, request.Prescription);
            }

            // 打印保护简化（2026-08-03）：打印后修改内容 → IsPrinted=false、PrintVersion++（提示重新打印）
            MedicalCaseServiceHelper.ResetPrintMarker(medicalCase);

            // 保存
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);
            _logger.LogInformation("[SVC] MedicalCase.Save completed - MedicalCaseId={MedicalCaseId}", medicalCaseId);
            return result;
        }

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
                return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

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
                return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

            var dto = _mapper.MapToMedicalCaseDetailDto(medicalCase);
            return LYBT.Shared.Models.Contracts.Common.Result<MedicalCaseDetailDto>.Success(dto);
        }

        /// <summary>
        /// 添加打印日志（成功回写打印状态，失败仅记录日志）
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<bool>> AddPrintLogAsync(
            Guid medicalCaseId,
            int printType,
            bool isSuccess,
            string? printerName,
            Guid operatorId,
            string operatorName,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
                return LYBT.Shared.Models.Contracts.Common.Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

            var now = DateTime.UtcNow;

            // 打印成功才回写医案打印状态
            if (isSuccess)
            {
                medicalCase.IsPrinted = true;
                medicalCase.PrintCount += 1;
                medicalCase.LastPrintedAt = now;
                medicalCase.PrintVersion += 1;

                await _repository.UpdateAsync(medicalCase, cancellationToken);
            }

            var printLog = new MedicalCasePrintLog
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
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
                return LYBT.Shared.Models.Contracts.Common.Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

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
