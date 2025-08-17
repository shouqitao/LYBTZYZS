using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Common;
using LYBT.Desktop.Core.Models.Herbs;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 前端中药材服务接口 - UltraThink四层架构（UI层）
    /// </summary>
    public interface IHerbService
    {
        #region 基础CRUD操作

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        Task<ServiceResult<HerbInfo>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbInfo>>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 获取所有药材
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetAllAsync();

        /// <summary>
        /// 创建药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> CreateAsync(HerbCreateDto dto);

        /// <summary>
        /// 更新药材
        /// </summary>
        Task<ServiceResult<HerbInfo>> UpdateAsync(Guid id, HerbUpdateDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        #endregion

        #region 批量操作

        /// <summary>
        /// 根据ID列表获取药材
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetByIdsAsync(List<Guid> ids);

        /// <summary>
        /// 搜索药材
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> SearchAsync(string keyword);

        /// <summary>
        /// 批量删除药材
        /// </summary>
        Task<ServiceResult<bool>> BatchDeleteAsync(List<Guid> ids);

        #endregion

        #region 业务方法

        /// <summary>
        /// 获取药材列表
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetListAsync(HerbPagedQueryDto? query = null);

        /// <summary>
        /// 搜索药材（分页）
        /// </summary>
        Task<PagedResult<HerbInfo>> SearchHerbsAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 获取药材列表（简化版）
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetHerbsAsync();

        /// <summary>
        /// 根据ID获取药材信息（可空返回）
        /// </summary>
        Task<HerbInfo?> GetByIdHerbInfoAsync(Guid id);

        #endregion

        #region 库存管理

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetOutOfStockHerbsAsync();

        /// <summary>
        /// 获取即将过期药材列表
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> GetExpiringHerbsAsync(int days = 30);

        #endregion

        #region 数据导入导出

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> ExportHerbsAsync();

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> SearchByNameAsync(string name);

        #endregion
    }
}