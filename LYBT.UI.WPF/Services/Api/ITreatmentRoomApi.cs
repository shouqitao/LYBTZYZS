using LYBT.Module.TreatmentRoom.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services.Api {
    public interface ITreatmentRoomApi {
        [Get("/api/TreatmentRoom")]
        Task<List<TreatmentRoomDto>> GetListAsync();

        [Get("/api/TreatmentRoom/{id}")]
        Task<TreatmentRoomDetailDto> GetByIdAsync(Guid id);

        [Post("/api/TreatmentRoom")]
        Task<ApiSuccessResponse> AddAsync([Body] TreatmentRoomCreateDto dto);

        [Put("/api/TreatmentRoom")]
        Task<ApiSuccessResponse> UpdateAsync([Body] TreatmentRoomEditDto dto);

        [Delete("/api/TreatmentRoom/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
