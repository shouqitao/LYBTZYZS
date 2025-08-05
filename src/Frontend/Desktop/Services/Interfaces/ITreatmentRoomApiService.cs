using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 治疗室API服务接口
    /// </summary>
    public interface ITreatmentRoomApiService
    {
        /// <summary>
        /// 获取治疗室单列表
        /// </summary>
        [Get("/api/v1/treatmentroom")]
        Task<Refit.ApiResponse<List<TreatmentRoomDto>>> GetListAsync();

        /// <summary>
        /// 分页获取治疗室列表
        /// </summary>
        [Get("/api/v1/treatmentroom/paged")]
        Task<Refit.ApiResponse<PaginatedResult<TreatmentRoomDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取治疗室单详情
        /// </summary>
        [Get("/api/v1/treatmentroom/{id}")]
        Task<Refit.ApiResponse<TreatmentRoomDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        [Post("/api/v1/treatmentroom")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] TreatmentRoomCreateDto treatmentRoomCreateDto);

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        [Put("/api/v1/treatmentroom")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] TreatmentRoomEditDto treatmentRoomEditDto);

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        [Delete("/api/v1/treatmentroom/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取治疗室单
        /// </summary>
        [Get("/api/v1/treatmentroom/status/{status}")]
        Task<Refit.ApiResponse<List<TreatmentRoomDto>>> GetByStatusAsync(string status);
    }
}