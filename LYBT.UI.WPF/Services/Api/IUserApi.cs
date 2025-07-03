using LYBT.Module.Users.Dtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services.Api {
    public interface IUserApi {
        [Get("/api/Users/search")]
        Task<SearchUsersResponse> SearchAsync([Query] UserQueryDto query);

        [Post("/api/Users/add")]
        Task<ApiSuccessResponse> AddAsync([Body] UserCreateDto user);

        [Put("/api/Users/update")]
        Task<ApiSuccessResponse> UpdateAsync([Body] UserEditDto user);

        [Post("/api/Users/disable/{id}")]
        Task<ApiSuccessResponse> DisableAsync(Guid id);

        [Post("/api/Users/resetPassword/{id}")]
        Task<ApiSuccessResponse> ResetPasswordAsync(Guid id, [Body] ResetPasswordDto dto);
    }
}
