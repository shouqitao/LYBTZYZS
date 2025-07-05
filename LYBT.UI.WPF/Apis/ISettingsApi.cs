using LYBT.Common.Models;
using LYBT.Module.Settings.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface ISettingsApi {
        [Get("/api/Settings")]
        Task<List<SettingsDto>> GetListAsync();

        [Get("/api/Settings/{id}")]
        Task<SettingsDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Settings")]
        Task<ApiSuccessResponse> AddAsync([Body] SettingsCreateDto dto);

        [Put("/api/Settings")]
        Task<ApiSuccessResponse> UpdateAsync([Body] SettingsEditDto dto);

        [Delete("/api/Settings/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
