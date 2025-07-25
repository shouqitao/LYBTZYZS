using LYBT.Module.Herbs.Models;

namespace LYBT.Module.Herbs.Interfaces {

    /// <summary>
    /// 药材仓储接口，定义药材相关数据库操作
    /// </summary>
    public interface IHerbRepository {

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<HerbModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        Task<List<HerbModel>> GetListAsync();

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<bool> AddAsync(HerbModel herb);

        /// <summary>
        /// 更新药材
        /// </summary>
        Task<bool> UpdateAsync(HerbModel herb);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量新增药材
        /// </summary>
        Task<bool> AddRangeAsync(List<HerbModel> herbs);

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<(List<HerbModel> list, int total)> GetPagedAsync(string? keyword, int page, int pageSize);
    }
}