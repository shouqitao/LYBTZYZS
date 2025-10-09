using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllAsync();
        Task<PatientDto> GetByIdAsync(Guid id);
        Task<PatientDto> CreateAsync(PatientDto patient);
        Task<PatientDto> UpdateAsync(PatientDto patient);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PatientDto>> SearchAsync(string keyword);

        /// <summary>
        /// 分页查询患者列表（服务端分页）- P0性能修复
        /// </summary>
        Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    }
}
