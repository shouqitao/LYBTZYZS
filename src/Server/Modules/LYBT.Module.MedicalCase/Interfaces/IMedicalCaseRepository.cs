using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<MedicalCaseEntity>
    {
        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据ID获取病案（包含所有关联数据）
        /// </summary>
        Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 获取分页列表（包含关联数据）
        /// </summary>
        Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据医生ID获取病案列表
        /// </summary>
        Task<List<MedicalCaseEntity>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 根据患者ID获取未完成的医案列表
        /// Issue #1568: 支持患者选择时自动检测并恢复未完成医案
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>未完成的医案列表（按创建时间降序）</returns>
        Task<List<MedicalCaseEntity>> GetIncompleteCasesByPatientIdAsync(Guid patientId);
    }
}
