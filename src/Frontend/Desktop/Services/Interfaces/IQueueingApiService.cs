using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Queueing;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 排队管理API服务接口
    /// </summary>
    public interface IQueueingApiService
    {
        /// <summary>
        /// 获取排队列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/queueing")]
        Task<Refit.ApiResponse<PaginatedResult<QueueingDto>>> GetListAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] string? queueType = null,
            [Query] QueueStatus? status = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null);

        /// <summary>
        /// 分页获取排队列表
        /// </summary>
        [Get("/api/v1/queueing/paged")]
        Task<Refit.ApiResponse<PaginatedResult<QueueingDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取排队详情
        /// </summary>
        [Get("/api/v1/queueing/{id}")]
        Task<Refit.ApiResponse<QueueingDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增排队
        /// </summary>
        [Post("/api/v1/queueing")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] QueueingCreateDto dto);

        /// <summary>
        /// 编辑排队
        /// </summary>
        [Put("/api/v1/queueing")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] QueueingEditDto dto);

        /// <summary>
        /// 删除排队
        /// </summary>
        [Delete("/api/v1/queueing/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 取消排队
        /// </summary>
        [Post("/api/v1/queueing/cancel/{id}")]
        Task<Refit.ApiResponse<object>> CancelAsync(Guid id);

        /// <summary>
        /// 完成排队
        /// </summary>
        [Post("/api/v1/queueing/complete/{id}")]
        Task<Refit.ApiResponse<object>> CompleteAsync(Guid id);

        /// <summary>
        /// 暂停排队
        /// </summary>
        [Post("/api/v1/queueing/hold/{id}")]
        Task<Refit.ApiResponse<object>> HoldAsync(Guid id);
    }
}