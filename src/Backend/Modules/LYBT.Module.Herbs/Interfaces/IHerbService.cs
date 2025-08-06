using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces {

    /// <summary>
    /// 药材业务服务接口（简化版）
    /// 只提供基础的药材信息维护功能，不包含库存管理
    /// </summary>
    public interface IHerbService {

        /// <summary>
        /// 获取药材详情
        /// </summary>
        Task<HerbDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        Task<List<HerbDto>> GetListAsync();

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<HerbDto?> AddAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材信息
        /// </summary>
        Task<bool> UpdateAsync(HerbUpdateDto dto);

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        Task<List<HerbDto>> SearchAsync(string keyword);

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        Task<List<HerbDto>> GetAvailableHerbsAsync();

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<int> ImportAsync(List<HerbImportDto> dtos);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<List<HerbDetailDto>> ExportAsync();
    }
}