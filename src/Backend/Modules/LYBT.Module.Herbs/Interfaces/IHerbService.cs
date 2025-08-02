using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

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
        /// 分页查询药材
        /// </summary>
        Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<bool> AddAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材
        /// </summary>
        Task<bool> UpdateAsync(HerbUpdateDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<int> ImportAsync(List<HerbImportDto> dtos);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<List<HerbDetailDto>> ExportAsync();

        // 需要在现有 IHerbService 接口中添加以下方法：

        /// <summary>
        /// 更新药材状态
        /// </summary>
        /// <param name="dto">状态更新DTO</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateStatusAsync(HerbStatusUpdateDto dto);

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        /// <param name="ids">药材ID列表</param>
        /// <param name="reason">更新原因</param>
        /// <returns>成功更新的数量</returns>
        Task<int> BatchUpdateStatusAsync(List<Guid> ids, string reason);

        /// <summary>
        /// 根据状态获取药材列表
        /// </summary>
        /// <param name="status">药材状态</param>
        /// <returns>药材列表</returns>
        Task<List<HerbDto>> GetByStatusAsync(HerbStatus status);

        /// <summary>
        /// 获取可用药材列表（状态为Active）
        /// </summary>
        /// <returns>可用药材列表</returns>
        Task<List<HerbDto>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        /// <returns>缺货药材列表</returns>
        Task<List<HerbDto>> GetOutOfStockHerbsAsync();

        /// <summary>
        /// 获取即将过期药材列表
        /// </summary>
        /// <param name="days">过期预警天数，默认30天</param>
        /// <returns>即将过期药材列表</returns>
        Task<List<HerbDto>> GetExpiringHerbsAsync(int days = 30);

        /// <summary>
        /// 检查药材状态并自动更新过期药材
        /// </summary>
        /// <returns>更新的药材数量</returns>
        Task<int> CheckAndUpdateExpiredHerbsAsync();

        /// <summary>
        /// 获取药材状态统计信息
        /// </summary>
        /// <returns>状态统计字典</returns>
        Task<Dictionary<HerbStatus, int>> GetStatusStatisticsAsync();

        /// <summary>
        /// 获取全部活动状态药材（用于处方检查）
        /// </summary>
        /// <returns>活动状态药材列表</returns>
        Task<List<HerbDto>> GetAllActiveHerbsAsync();
    }
}