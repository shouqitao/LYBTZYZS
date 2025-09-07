using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Interfaces.Services
{

    /// <summary>
    /// 中药材服务接口 - UltraThink双层架构精简标准（小诊所适用）
    /// </summary>
    public interface IHerbService
    {

        #region 查询操作 - QueryService专业负责

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 搜索药材（按名称）
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 批量获取药材（用于处方）
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);

        #endregion 查询操作 - QueryService专业负责

        #region 业务操作 - BusinessService专业负责

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
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 启用药材
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);

        /// <summary>
        /// 禁用药材
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        #endregion 业务操作 - BusinessService专业负责

        #region 批量操作 - 必需功能（用户明确需求）

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query);

        #endregion 批量操作 - 必需功能（用户明确需求）
    }
}
