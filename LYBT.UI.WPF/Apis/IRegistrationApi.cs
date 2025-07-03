using LYBT.Module.Registration.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IRegistrationApi {
        [Get("/api/Registration")]
        Task<List<RegistrationDto>> GetListAsync();

        [Get("/api/Registration/{id}")]
        Task<RegistrationDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Registration")]
        Task<ApiSuccessResponse> AddAsync([Body] RegistrationCreateDto dto);

        [Put("/api/Registration")]
        Task<ApiSuccessResponse> UpdateAsync([Body] RegistrationEditDto dto);

        [Delete("/api/Registration/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
