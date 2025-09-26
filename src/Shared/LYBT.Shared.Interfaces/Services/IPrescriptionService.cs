using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 处方服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 分页查询处方
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方信息
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto);

        /// <summary>
        /// 删除处方（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}