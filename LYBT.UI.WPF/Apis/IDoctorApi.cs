using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IDoctorApi {
        [Get("/api/Doctors/search")]
        Task<List<DoctorDto>> SearchAsync([Query] string keyword);

        [Get("/api/Doctors/{id}")]
        Task<DoctorDetailDto> GetByIdAsync(Guid id);

        [Post("/api/Doctors/add")]
        Task<ApiSuccessResponse> AddAsync([Body] DoctorCreateDto dto);

        [Put("/api/Doctors/update")]
        Task<ApiSuccessResponse> UpdateAsync([Body] DoctorEditDto dto);

        [Put("/api/Doctors/disable/{id}")]
        Task<ApiSuccessResponse> DisableAsync(Guid id);

        [Put("/api/Doctors/enable/{id}")]
        Task<ApiSuccessResponse> EnableAsync(Guid id);

        [Post("/api/Doctors/paged")]
        Task<PagedResultDto<DoctorDto>> GetPagedAsync([Body] DoctorQueryDto query);

        [Put("/api/Doctors/batch-disable")]
        Task<ApiSuccessResponse> BatchDisableAsync([Body] BatchIdsDto dto);

        [Put("/api/Doctors/batch-enable")]
        Task<ApiSuccessResponse> BatchEnableAsync([Body] BatchIdsDto dto);

        [Put("/api/Doctors/reset-password/{id}")]
        Task<ApiSuccessResponse> ResetPasswordAsync(Guid id, [Body] ResetPasswordDto dto);

        [Put("/api/Doctors/change-password")]
        Task<ApiSuccessResponse> ChangePasswordAsync([Body] ChangePasswordDto dto);

        [Get("/api/Doctors/roles")]
        Task<List<string>> GetRolesAsync();

    }
}
