using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供处方记录的只读查询功能、价格计算和打印格式生成
    /// 所有Write操作必须通过MedicalCaseService聚合根进行
    /// OpenSpec: consolidate-medicalcase-queries - 跨医案查询已迁移至MedicalCaseQueryService
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IPrescriptionNumberService _numberService;
        private readonly PrescriptionMapper _mapper = new();
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IPrescriptionRepository repository,
            IPrescriptionNumberService numberService,
            ILogger<PrescriptionService> logger)
        {
            _repository = repository;
            _numberService = numberService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<PrescriptionDetailDto> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("[SVC] Prescription.GetById started - PrescriptionId={Id}", id);

            // 使用优化后的查询方法，包含处方项
            var entity = await _repository.GetByIdWithDetailsAsync(id)
                ?? throw NotFoundException.Prescription(id);

            return _mapper.ToDetailDto(entity);
        }

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync, CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <inheritdoc/>
        public async Task<List<PrescriptionDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            _logger.LogDebug("[SVC] Prescription.GetByMedicalCaseId started - MedicalCaseId={MedicalCaseId}", medicalCaseId);

            // 使用优化后的查询方法，直接查询并包含Items集合
            var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);

            // 转换为DTO
            return _mapper.ToDetailDtos(prescriptions.ToList());
        }

        /// <summary>
        /// 计算处方总价 - 简化的价格计算逻辑
        /// </summary>
        /// <param name="items">处方项列表</param>
        /// <param name="dosageCount">处方帖数</param>
        /// <param name="discount">折扣</param>
        /// <returns>总价</returns>
        private decimal CalculateTotalAmount(IEnumerable<LYBT.Entities.Prescriptions.PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
        {
            decimal total = 0;

            foreach (var item in items)
            {
                // 基础价格计算：单价 × 数量 × 帖数
                var itemTotal = item.UnitPrice * item.Dosage * dosageCount;
                total += itemTotal;
            }

            // 应用折扣
            return total * discount;
        }

        // ========== 跨医案查询方法已迁移（OpenSpec: consolidate-medicalcase-queries）==========
        // SearchPrescriptionsAsync 已删除 - 请使用 MedicalCaseQueryService.SearchMedicalCasesAsync
        // GetPatientRecentPrescriptionsAsync 已删除 - 请使用 MedicalCaseQueryService.GetPatientRecentMedicalCasesAsync
        // LoadMedicalCasesAsync 已删除 - 级联清理（唯一调用者被删除）
        // LoadPatientsAsync 已删除 - 级联清理（唯一调用者被删除）
    }
}
