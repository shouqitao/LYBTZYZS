using LYBT.Models.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Interfaces {

    /// <summary>
    /// 智能处方服务接口
    /// </summary>
    public interface IIntelligentPrescriptionService {

        /// <summary>
        /// 智能组合多个验方模板生成处方
        /// </summary>
        Task<PrescriptionCompositionResult> ComposeFromFormulasAsync(List<Guid> formulaIds, int dosageCount = 7);

        /// <summary>
        /// 智能重复药材检测和处理
        /// </summary>
        PrescriptionDuplicateCheckResult DetectDuplicateHerbs(List<PrescriptionItemModel> items);

        /// <summary>
        /// 检查药材库存状态
        /// </summary>
        Task<HerbAvailabilityCheckResult> CheckHerbAvailabilityAsync(List<PrescriptionItemModel> items);

        /// <summary>
        /// 计算处方价格和重量
        /// </summary>
        PrescriptionPriceCalculationResult CalculatePrescriptionPrice(List<PrescriptionItemModel> items, int dosageCount);

        /// <summary>
        /// 生成处方智能建议
        /// </summary>
        Task<PrescriptionSuggestionResult> GeneratePrescriptionSuggestionsAsync(string diagnosis, List<string> symptoms, Guid? doctorId = null);
    }
}