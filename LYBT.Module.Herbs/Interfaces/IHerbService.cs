using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.Herbs.Interfaces {

    /// <summary>
    /// 药材业务服务接口
    /// </summary>
    public interface IHerbService {

        /// <summary>
        /// 获取药材详情
        /// </summary>
        Task<HerbDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取药材列表
        /// </summary>
        Task<List<HerbDto>> GetListAsync();

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<bool> AddAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材
        /// </summary>
        Task<bool> UpdateAsync(HerbEditDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}