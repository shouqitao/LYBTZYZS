using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
    }
}