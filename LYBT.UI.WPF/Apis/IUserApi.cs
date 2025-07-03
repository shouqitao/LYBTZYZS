using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IUserApi {
        [Get("/api/Users/search")]
        Task<SearchUsersResponse> SearchAsync([Query] UserQueryDto query);

        [Post("/api/Users/add")]
        Task<ApiSuccessResponse> AddAsync([Body] UserCreateDto user);

        [Put("/api/Users/update")]
        Task<ApiSuccessResponse> UpdateAsync([Body] UserEditDto user);

        [Post("/api/Users/disable/{id}")]
        Task<ApiSuccessResponse> DisableAsync(Guid id);

        [Post("/api/Users/enable/{id}")]
        Task<ApiSuccessResponse> EnableAsync(Guid id);

        [Post("/api/Users/batchDisable")]
        Task<ApiSuccessResponse> BatchDisableAsync([Body] BatchIdsDto dto);

        [Post("/api/Users/batchEnable")]
        Task<ApiSuccessResponse> BatchEnableAsync([Body] BatchIdsDto dto);

        [Post("/api/Users/resetPassword/{id}")]
        Task<ApiSuccessResponse> ResetPasswordAsync(Guid id, [Body] ResetPasswordDto dto);

        [Post("/api/Users/changePassword")]
        Task<ApiSuccessResponse> ChangePasswordAsync([Body] ChangePasswordDto dto);

        [Get("/api/Users/getRoles")]
        Task<List<UserRole>> GetRolesAsync();

        [Get("/api/Users/getById/{id}")]
        Task<UserDto> GetByIdAsync(Guid id);
    }
}
