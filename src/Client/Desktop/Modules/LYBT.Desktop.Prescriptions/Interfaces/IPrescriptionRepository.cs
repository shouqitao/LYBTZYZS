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
        Task<PrescriptionDto> GetByIdAsync(Guid id);
        Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto);
        Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者最近处方列表 (ENTRY-13)
        /// </summary>
        Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(Guid patientId, int count = 5);

        /// <summary>
        /// 搜索处方 (ENTRY-14)
        /// </summary>
        Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(string? patientName = null, string? symptomKeyword = null);

        /// <summary>
        /// 复制处方到新处方 (ENTRY-15)
        /// </summary>
        Task<ServiceResult<string>> ClonePrescriptionAsync(Guid sourcePrescriptionId, Guid targetPrescriptionId);
    }
}
