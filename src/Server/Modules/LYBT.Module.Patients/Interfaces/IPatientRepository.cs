using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Module.Patients.Repositories;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 病人仓储接口 - 优化版，包含查询优化方法
    /// </summary>
    public interface IPatientRepository : IRepository<Patient>
    {
        /// <summary>
        /// 根据姓名查找病人
        /// </summary>
        Task<Patient?> GetByNameAsync(string name);

        /// <summary>
        /// 获取患者及其就诊记录
        /// </summary>
        Task<Patient?> GetPatientWithVisitsAsync(Guid patientId);

        /// <summary>
        /// 获取患者摘要列表（分页）
        /// </summary>
        Task<PaginatedList<PatientSummary>> GetPatientSummariesAsync(int pageIndex, int pageSize);

        /// <summary>
        /// 搜索患者（支持分页）
        /// </summary>
        Task<PaginatedList<Patient>> SearchPatientsAsync(string? searchTerm, int pageIndex, int pageSize);

        /// <summary>
        /// 批量获取患者
        /// </summary>
        Task<List<Patient>> GetPatientsByIdsAsync(IEnumerable<Guid> patientIds);

        /// <summary>
        /// 检查手机号是否存在
        /// </summary>
        Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null);

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        Task<PatientStatistics> GetStatisticsAsync();

        /// <summary>
        /// 批量更新最后就诊时间
        /// </summary>
        Task UpdateLastVisitDateAsync(IEnumerable<Guid> patientIds, DateTime visitDate);
    }
}