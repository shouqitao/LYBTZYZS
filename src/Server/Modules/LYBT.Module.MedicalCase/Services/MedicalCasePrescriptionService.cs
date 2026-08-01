using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading;
using EC = LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案处方服务 - Prescription 生命周期操作
    /// U3-1: 从 MedicalCaseCommandService 拆分，聚焦处方三态流程（标记/创建/更新/删除/复制）
    /// </summary>
    public class MedicalCasePrescriptionService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ICrossModuleService _crossModule;
        private readonly MedicalCaseMapper _mapper;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<MedicalCasePrescriptionService> _logger;
        private readonly PrescriptionItemService _itemService;

        public MedicalCasePrescriptionService(
            IMedicalCaseRepository repository,
            ICrossModuleService crossModule,
            MedicalCaseMapper mapper,
            ICacheInvalidationService cacheInvalidation,
            ILogger<MedicalCasePrescriptionService> logger,
            PrescriptionItemService itemService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _crossModule = crossModule ?? throw new ArgumentNullException(nameof(crossModule));
            _mapper = mapper;
            _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// </summary>
        public async Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.SetPrescriptionFlag - MedicalCaseId={MedicalCaseId} NeedsPrescription={NeedsPrescription}",
                medicalCaseId, needsPrescription);

            // 获取聚合根
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.SetPrescriptionFlag → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, currentUserId, isAdmin, "SetPrescriptionFlag", _logger);

            // 更新NeedsPrescription标志
            medicalCase.NeedsPrescription = needsPrescription;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // T5-P2-12: 标记不需要处方时，软删除已有处方
            if (!needsPrescription)
            {
                await _itemService.SoftDeletePrescriptionIfExists(medicalCase);
            }

            // 保存
            var result = await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);
            return result;
        }

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612: 通过聚合根创建Prescription
        /// 业务规则：AR-001（聚合根约束）、AR-003（一诊一方约束）
        /// </summary>
        public async Task<Prescription?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionInputDto request,
            CancellationToken cancellationToken = default)
        {
            return await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
                () => ExecuteCreatePrescriptionAsync(medicalCaseId, request, cancellationToken),
                "CreatePrescription", _logger);
        }

        /// <summary>
        /// 复制历史处方到新医案
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>> CopyHistoricalPrescriptionAsync(
            Guid sourceMedicalCaseId,
            Guid targetMedicalCaseId,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
        {
            return await MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync(
                () => ExecuteCopyHistoricalPrescriptionAsync(sourceMedicalCaseId, targetMedicalCaseId, currentUserId, cancellationToken),
                "CopyHistoricalPrescription", _logger);
        }

        /// <summary>
        /// 实际执行历史处方复制逻辑
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>> ExecuteCopyHistoricalPrescriptionAsync(
            Guid sourceMedicalCaseId,
            Guid targetMedicalCaseId,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            // 1) fetch source with prescription
            var sourceCase = await _repository.GetByIdWithDetailsAsync(sourceMedicalCaseId, cancellationToken);
            if (sourceCase == null)
            {
                return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Failure("源医案不存在");
            }
            if (sourceCase.Prescription == null)
            {
                return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Failure("源医案没有处方");
            }

            // 2) fetch target and validate
            var targetCase = await _repository.GetByIdWithDetailsAsync(targetMedicalCaseId, cancellationToken);
            if (targetCase == null)
            {
                return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Failure("目标医案不存在");
            }
            if (targetCase.NeedsPrescription != true)
            {
                return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Failure("目标医案不需要处方");
            }
            if (targetCase.Prescription != null)
            {
                return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Failure("目标医案已有处方");
            }

            // 3) copy prescription with new IDs
            var sourcePrescription = sourceCase.Prescription!
;
            var newPrescription = new Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = targetMedicalCaseId,
                PrescriptionNumber = await _itemService.GeneratePrescriptionNumberAsync(cancellationToken),  // TODO: 价格刷新在后续实现
                Remark = sourcePrescription.Remark,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            // 4) 复制处方明细（价格刷新 TODO 已在 US-MC-016 记录）
            var newItems = new List<PrescriptionItem>();
            foreach (var sourceItem in sourcePrescription.Items)
            {
                newItems.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = newPrescription.Id,
                    HerbId = sourceItem.HerbId,
                    HerbName = sourceItem.HerbName,
                    Dosage = sourceItem.Dosage,
                    Unit = sourceItem.Unit,
                    UnitPrice = sourceItem.UnitPrice, // TODO: refresh price from herb catalog
                    Remark = sourceItem.Remark
                });
            }

            newPrescription.Items = newItems;

            // 5) attach to target case and persist
            targetCase.Prescription = newPrescription;
            targetCase.UpdatedAt = DateTime.UtcNow;
            targetCase.UpdatedBy = currentUserId;

            await _repository.UpdateAsync(targetCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.CopyHistoricalPrescription completed - SourceCaseId={SourceCaseId} TargetCaseId={TargetCaseId}", sourceMedicalCaseId, targetMedicalCaseId);

            // 6) map to DTO and return as successful result
            var dto = _mapper.ToPrescriptionDetailDto(newPrescription);
            return LYBT.Shared.Models.Contracts.Common.Result<PrescriptionDetailDto>.Success(dto);
        }

        /// <summary>
        /// 执行单次处方创建
        /// </summary>
        public async Task<Prescription?> ExecuteCreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionInputDto request,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId, cancellationToken);
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
            prescription.PrescriptionNumber = await _itemService.GeneratePrescriptionNumberAsync(cancellationToken);  // T5-P2-13
            prescription.MedicalCaseId = medicalCaseId;
            prescription.CreatedAt = DateTime.UtcNow;
            prescription.UpdatedAt = DateTime.UtcNow;

            // T2-S4-02: 使用统一的CreatePrescriptionItemsAsync确保UnitPrice自动填充
            prescription.Items = await _itemService.CreatePrescriptionItemsAsync(prescription.Id, request, cancellationToken);

            medicalCase.Prescription = prescription;
            medicalCase.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

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
            string? editReason = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.UpdatePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescriptionId);

            var medicalCase = await _repository.GetByIdWithDetailsFreshAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return null;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanEdit(medicalCase, currentUserId, isAdmin, "UpdatePrescription", _logger);

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("[SVC] MedicalCase.UpdatePrescription → PrescriptionNotFound - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCaseId, prescriptionId);
                return null;
            }

            // 通过Mapperly更新Prescription子实体（不包含Items）
            _mapper.UpdatePrescriptionEntity(request, medicalCase.Prescription);
            medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // T2-S4-02: 使用统一的CreatePrescriptionItemsAsync确保UnitPrice自动填充
            if (request.Items != null)
            {
                medicalCase.Prescription.Items.Clear();
                foreach (var item in await _itemService.CreatePrescriptionItemsAsync(prescriptionId, request, cancellationToken))
                {
                    medicalCase.Prescription.Items.Add(item);
                }
            }

            await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);

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
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.DeletePrescription - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                medicalCaseId, prescriptionId);

            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase == null)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → NotFound - MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return false;
            }

            // 权限检查
            MedicalCaseServiceHelper.EnsureCanDelete(medicalCase, currentUserId, isAdmin, "DeletePrescription", _logger);

            // 验证Prescription存在且ID匹配
            if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
            {
                _logger.LogWarning("[SVC] MedicalCase.DeletePrescription → PrescriptionNotFound - PrescriptionId={PrescriptionId}", prescriptionId);
                return false;
            }

            // 软删除Prescription
            medicalCase.Prescription.IsDeleted = true;
            medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;

            // 清空导航属性（保持聚合根一致性）
            medicalCase.Prescription = null;
            medicalCase.UpdatedAt = DateTime.UtcNow;

            // 通过聚合根保存
            await _repository.UpdateAsync(medicalCase, cancellationToken);
            await _cacheInvalidation.InvalidateAsync("medicalcases", cancellationToken);
            return true;
        }
    }
}
