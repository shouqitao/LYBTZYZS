using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材引用查询仓储 - 封装跨聚合的引用检查查询
    /// Task 6: Repository 规范统一 (Wave 4)
    /// </summary>
    public interface IHerbReferenceRepository
    {
        /// <summary>获取处方引用计数</summary>
        Task<int> GetPrescriptionReferenceCountAsync(Guid herbId, CancellationToken ct = default);

        /// <summary>获取验方引用计数</summary>
        Task<int> GetFormulaReferenceCountAsync(Guid herbId, CancellationToken ct = default);

        /// <summary>获取最近N条处方引用记录</summary>
        Task<List<PrescriptionReferenceDto>> GetRecentPrescriptionReferencesAsync(Guid herbId, int take, CancellationToken ct = default);
    }
}
