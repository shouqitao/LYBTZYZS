using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 药材服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IHerbService
    {
        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新药材
        /// </summary>
        Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);

        /// <summary>
        /// 更新药材信息
        /// </summary>
        Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
