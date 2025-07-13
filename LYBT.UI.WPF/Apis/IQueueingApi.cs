using LYBT.Common.Models;
using LYBT.Module.Queueing.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IQueueingApi {
        [Get("/api/Queueing")]
        Task<List<QueueingDto>> GetListAsync();

        [Get("/api/Queueing/{id}")]
        Task<QueueingDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Queueing")]
        Task<ApiSuccessResponse> AddAsync([Body] QueueingCreateDto dto);

        [Put("/api/Queueing")]
        Task<ApiSuccessResponse> UpdateAsync([Body] QueueingEditDto dto);

        [Delete("/api/Queueing/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);

        [Post("/api/Queueing/complete/{id}")]
        Task<ApiSuccessResponse> CompleteAsync(Guid id);

        [Post("/api/Queueing/hold/{id}")]
        Task<ApiSuccessResponse> HoldAsync(Guid id);
    }
}
