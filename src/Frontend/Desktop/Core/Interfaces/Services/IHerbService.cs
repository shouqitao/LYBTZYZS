using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 药材服务接口
    /// </summary>
    public interface IHerbService
    {
        Task<ApiResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null);
        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<Models.Common.PagedResult<HerbInfo>> SearchHerbsAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 获取药材列表
        /// </summary>
        Task<List<HerbInfo>> GetHerbsAsync();

        /// <summary>
        /// 获取药材详情
        /// </summary>
        Task<HerbInfo?> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材
        /// </summary>
        Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<ServiceResult> DeleteHerbAsync(Guid id);

        /// <summary>
        /// 更新药材状态
        /// </summary>
        Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto);


        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        Task<List<HerbInfo>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        Task<List<HerbInfo>> GetOutOfStockHerbsAsync();

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        Task<List<HerbInfo>> GetExpiringHerbsAsync(int days = 30);

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        Task<Dictionary<int, int>> GetStatisticsAsync();

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        Task<List<HerbInfo>> ExportHerbsAsync();

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        Task<ServiceResult<List<HerbInfo>>> SearchByNameAsync(string name);
    }
}