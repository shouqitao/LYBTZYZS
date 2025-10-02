using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPrescriptionRepository : IRepository<Prescription>
    {
        /// <summary>
        /// 根据ID获取处方（包含处方项和药材信息）
        /// </summary>
        Task<Prescription> GetByIdWithItemsAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据病案ID获取处方
        /// </summary>
        Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}
