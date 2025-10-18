using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IPrescriptionRepository
    {
        Task<PagedResult<PrescriptionDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<PrescriptionDto?> GetByIdAsync(Guid id);
        Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto);
        Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}
