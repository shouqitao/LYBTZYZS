using LYBT.Module.Users.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services.Api {
    public interface IUserApi {
        [Get("/api/Users")]
        Task<List<UserModel>> GetUsersAsync([Query] string keyword = "");

        [Post("/api/Users")]
        Task<bool> AddUserAsync([Body] UserModel user);

        [Put("/api/Users/{id}")]
        Task<bool> UpdateUserAsync(Guid id, [Body] UserModel user);

        [Post("/api/Users/{id}/Disable")]
        Task<bool> DisableUserAsync(Guid id);

        [Post("/api/Users/{id}/ResetPassword")]
        Task<bool> ResetPasswordAsync(Guid id);
    }
}
