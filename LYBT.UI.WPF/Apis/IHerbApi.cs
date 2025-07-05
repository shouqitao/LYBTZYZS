using LYBT.Module.Herbs.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IHerbApi {
        [Get("/api/Herb")]
        Task<List<HerbDto>> GetListAsync();

        [Get("/api/Herb/{id}")]
        Task<HerbDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Herb")]
        Task<ApiSuccessResponse> AddAsync([Body] HerbCreateDto dto);

        [Put("/api/Herb")]
        Task<ApiSuccessResponse> UpdateAsync([Body] HerbEditDto dto);

        [Delete("/api/Herb/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
