using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    /// <summary>
    /// 处方数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IPrescriptionRepository
    {
        Task<PagedResult<PrescriptionDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<PrescriptionDto> GetByIdAsync(Guid id);
        Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto);
        Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 导入验方到处方
        /// ENTRY-10: 集成导入命令到PrescriptionComposerViewModel
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="formulaId">验方ID</param>
        Task<ServiceResult> ImportFormulaAsync(Guid prescriptionId, Guid formulaId);
    }
}
