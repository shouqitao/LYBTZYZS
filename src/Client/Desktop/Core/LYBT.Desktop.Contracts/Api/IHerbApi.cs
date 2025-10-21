using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 草药API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IHerbApi
    {
        /// <summary>
        /// 获取草药列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/herbs")]
        Task<ApiResponse<PagedResult<HerbDto>>> GetHerbsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取草药详情
        /// </summary>
        [Refit.Get("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建草药
        /// </summary>
        [Refit.Post("/api/v1/herbs")]
        Task<ApiResponse<HerbDto>> CreateHerbAsync([Refit.Body] HerbCreateDto request);

        /// <summary>
        /// 更新草药
        /// </summary>
        [Refit.Put("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDto>> UpdateHerbAsync(Guid id, [Refit.Body] HerbUpdateDto request);

        /// <summary>
        /// 删除草药
        /// </summary>
        [Refit.Delete("/api/v1/herbs/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteHerbAsync(Guid id);
    }
}
