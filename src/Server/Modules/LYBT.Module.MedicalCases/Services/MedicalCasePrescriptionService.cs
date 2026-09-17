using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Caching;
using LYBT.Module.MedicalCases.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案处方服务 - Prescription 生命周期操作
    /// U3-1: 从 MedicalCaseCommandService 拆分，聚焦处方三态流程（标记/创建/更新/删除/复制）
    /// P2-9-5 评估：历史处方价格已在 PrescriptionItem.UnitPrice 快照（创建时拷贝 Herb.Price），FormulaHerbItem 仅模板不快照，处方价格不受验方后续调价影响。
    /// </summary>
    public class MedicalCasePrescriptionService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<MedicalCasePrescriptionService> _logger;
        private readonly PrescriptionItemService _itemService;

        public MedicalCasePrescriptionService(
            IMedicalCaseRepository repository,
            ICacheInvalidationService cacheInvalidation,
            ILogger<MedicalCasePrescriptionService> logger,
            PrescriptionItemService itemService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
            MedicalCaseServiceHelper.EnsureCanOperate(medicalCase, currentUserId, isAdmin, "SetPrescriptionFlag", _logger);

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
    }
}
