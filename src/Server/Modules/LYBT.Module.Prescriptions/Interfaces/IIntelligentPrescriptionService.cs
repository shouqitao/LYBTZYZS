using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 智能处方服务接口 - 核心配伍和验方组合功能
    /// </summary>
    public interface IIntelligentPrescriptionService
    {
        /// <summary>
        /// 智能组合多个验方模板生成处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> ComposeFromFormulasAsync(List<Guid> formulaIds, int dosageCount = 7);

        /// <summary>
        /// 智能重复药材检测和处理
        /// </summary>
        ServiceResult<List<PrescriptionItemDto>> DetectDuplicateHerbs(List<PrescriptionItemDto> items);

        /// <summary>
        /// 计算处方价格和重量
        /// </summary>
        ServiceResult<PrescriptionCalculationDto> CalculatePrescriptionPrice(List<PrescriptionItemDto> items, int dosageCount);
    }
}
