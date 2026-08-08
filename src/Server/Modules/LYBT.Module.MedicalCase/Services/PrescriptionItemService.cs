using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 处方条目服务 - 处方创建/更新/软删除及明细处理
    /// U3-1: 从 MedicalCaseCommandService 拆分，聚焦 Prescription 内部操作
    /// </summary>
    public class PrescriptionItemService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ICrossModuleService _crossModule;
        private readonly ILogger<PrescriptionItemService> _logger;

        public PrescriptionItemService(
            IMedicalCaseRepository repository,
            ICrossModuleService crossModule,
            ILogger<PrescriptionItemService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _crossModule = crossModule ?? throw new ArgumentNullException(nameof(crossModule));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理处方更新(创建/更新/软删除)
        /// consolidate-code-quality: 从SaveAsync提取，降低圈复杂度
        /// </summary>
        public async Task HandlePrescriptionUpdateAsync(
            MedicalCase medicalCase,
            PrescriptionInputDto prescriptionDto,
            CancellationToken cancellationToken = default)
        {
            medicalCase.NeedsPrescription = prescriptionDto.NeedsPrescription;

            if (!prescriptionDto.NeedsPrescription)
            {
                await SoftDeletePrescriptionIfExists(medicalCase);
                return;
            }

            if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
            {
                await CreateNewPrescriptionAsync(medicalCase, prescriptionDto, cancellationToken);
            }
            else
            {
                await UpdateExistingPrescriptionAsync(medicalCase.Prescription, prescriptionDto, cancellationToken);
            }
        }

        /// <summary>
        /// 软删除现有处方
        /// </summary>
        public Task SoftDeletePrescriptionIfExists(MedicalCase medicalCase)
        {
            if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
            {
                medicalCase.Prescription.IsDeleted = true;
                medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionSoftDeleted - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId}",
                    medicalCase.Id, medicalCase.Prescription.Id);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 创建新处方
        /// </summary>
        public async Task CreateNewPrescriptionAsync(
            MedicalCase medicalCase,
            PrescriptionInputDto prescriptionDto,
            CancellationToken cancellationToken = default)
        {
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                PrescriptionNumber = await GeneratePrescriptionNumberAsync(cancellationToken),  // T5-P2-13: 自动生成处方编号
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
            prescription.Items = await CreatePrescriptionItemsAsync(prescription.Id, prescriptionDto, cancellationToken);

            medicalCase.Prescription = prescription;
            _logger.LogInformation("[SVC] MedicalCase.Save → PrescriptionCreated - MedicalCaseId={MedicalCaseId} PrescriptionId={PrescriptionId} ItemCount={ItemCount}",
                medicalCase.Id, prescription.Id, prescription.Items.Count);
        }

        /// <summary>
        /// 更新现有处方
        /// </summary>
        public async Task UpdateExistingPrescriptionAsync(
            Prescription prescription,
            PrescriptionInputDto prescriptionDto,
            CancellationToken cancellationToken = default)
        {
            prescription.DosageCount = prescriptionDto.DosageCount;
            prescription.Usage = prescriptionDto.Usage;
            prescription.Advice = prescriptionDto.Advice;
            prescription.ReferencedFormulas = prescriptionDto.ReferencedFormulas;
            prescription.Discount = prescriptionDto.Discount;
            prescription.Remark = prescriptionDto.Remark;
            prescription.UpdatedAt = DateTime.UtcNow;

            prescription.Items.Clear();
            foreach (var item in await CreatePrescriptionItemsAsync(prescription.Id, prescriptionDto, cancellationToken))
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
        public async Task<List<LYBT.Entities.Prescriptions.PrescriptionItem>> CreatePrescriptionItemsAsync(
            Guid prescriptionId,
            PrescriptionInputDto prescriptionDto,
            CancellationToken cancellationToken = default)
        {
            var items = new List<LYBT.Entities.Prescriptions.PrescriptionItem>();

            if (prescriptionDto.Items == null || !prescriptionDto.Items.Any()) return items;

            var allHerbIds = prescriptionDto.Items.Select(i => i.HerbId).Distinct().ToList();

            // AD-02: 过滤禁用药材，禁止加入处方
            var disabledHerbIds = await _crossModule.GetDisabledHerbIdsAsync(allHerbIds, cancellationToken);
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
                herbPrices = await _crossModule.GetHerbPricesAsync(herbIdsNeedingPrice, cancellationToken);
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

        /// <summary>
        /// 生成处方编号（格式：RX + 年月日 + 序号）
        /// T5-P2-13
        /// </summary>
        public async Task<string> GeneratePrescriptionNumberAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            var dateStr = today.ToString("yyyyMMdd");
            var prefix = $"RX{dateStr}";

            var count = await _repository.CountPrescriptionsByPrefixAsync(prefix, cancellationToken);
            return $"{prefix}{(count + 1):D4}";
        }
    }
}
