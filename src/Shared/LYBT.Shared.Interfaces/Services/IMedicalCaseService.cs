using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 医疗案例服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);

        /// <summary>
        /// 更新医疗案例信息
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}