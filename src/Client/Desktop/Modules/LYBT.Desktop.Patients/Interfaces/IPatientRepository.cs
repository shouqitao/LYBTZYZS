using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllAsync();
        Task<PatientDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者（使用CreateDto）
        /// </summary>
        Task<PatientDto> CreateAsync(PatientInputDto patient);

        /// <summary>
        /// 更新患者信息（使用UpdateDto）
        /// </summary>
        Task<PatientDto> UpdateAsync(PatientInputDto patient);

        Task<bool> DeleteAsync(Guid id);
        Task<List<PatientDto>> SearchAsync(string keyword);

        /// <summary>
        /// 分页查询患者列表（服务端分页）- P0性能修复
        /// </summary>
        Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    }
}
