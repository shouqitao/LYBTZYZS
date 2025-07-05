using LYBT.Module.Pharmacy.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IPharmacyApi {
        [Get("/api/Pharmacy/waiting")]
        Task<List<PharmacyDto>> GetWaitingListAsync();

        [Get("/api/Pharmacy")]
        Task<List<PharmacyDto>> GetListAsync();

        [Get("/api/Pharmacy/{id}")]
        Task<PharmacyDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Pharmacy")]
        Task<ApiSuccessResponse> AddAsync([Body] PharmacyCreateDto dto);

        [Put("/api/Pharmacy")]
        Task<ApiSuccessResponse> UpdateAsync([Body] PharmacyEditDto dto);

        [Delete("/api/Pharmacy/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);

        [Post("/api/Pharmacy/{id}/prepared")]
        Task<ApiSuccessResponse> MarkAsPreparedAsync(Guid id);
    }
}
