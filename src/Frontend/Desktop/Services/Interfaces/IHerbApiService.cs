using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 药材API服务接口
    /// </summary>
    public interface IHerbApiService
    {
        /// <summary>
        /// 分页查询药材
        /// </summary>
        [Post("/api/v1/herbs/paged")]
        Task<Refit.ApiResponse<PaginatedResult<HerbDto>>> GetPagedHerbsAsync([Body] HerbPagedQueryDto query);

        /// <summary>
        /// 获取药材列表
        /// </summary>
        [Get("/api/v1/herbs")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetHerbsAsync();

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [Get("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建药材
        /// </summary>
        [Post("/api/v1/herbs")]
        Task<Refit.ApiResponse<HerbDto>> CreateHerbAsync([Body] HerbCreateDto dto);

        /// <summary>
        /// 更新药材
        /// </summary>
        [Put("/api/v1/herbs")]
        Task<Refit.ApiResponse<object>> UpdateHerbAsync([Body] HerbUpdateDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        [Delete("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<object>> DeleteHerbAsync(Guid id);


        /// <summary>
        /// 更新药材状态
        /// </summary>
        [Put("/api/v1/herbs/{id}/status")]
        Task<Refit.ApiResponse<object>> UpdateStatusAsync(Guid id, [Body] CommonStatusUpdateDto dto);

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        [Get("/api/v1/herbs/available")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        [Get("/api/v1/herbs/out-of-stock")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetOutOfStockHerbsAsync();

        /// <summary>
        /// 获取即将过期药材列表
        /// </summary>
        [Get("/api/v1/herbs/expiring")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetExpiringHerbsAsync([Query] int days = 30);

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        [Get("/api/v1/herbs/statistics")]
        Task<Refit.ApiResponse<Dictionary<int, int>>> GetStatisticsAsync();

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [Post("/api/v1/herbs/import")]
        Task<Refit.ApiResponse<int>> ImportHerbsAsync([Body] List<HerbImportDto> herbs);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [Get("/api/v1/herbs/export")]
        Task<Refit.ApiResponse<List<HerbDetailDto>>> ExportHerbsAsync();
    }
}