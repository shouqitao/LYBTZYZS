using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using Refit;
using System.Net.Http;

namespace LYBT.UI.WPF.Services {
    public interface IUserManagementService {
        Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query);
        Task<List<UserRole>> GetRolesAsync();
        Task<bool> AddAsync(UserCreateDto dto);
        Task<bool> UpdateAsync(UserEditDto dto);
        Task<bool> DisableAsync(Guid id);
        Task<bool> EnableAsync(Guid id);
    }

    public class UserManagementService : IUserManagementService {
        private readonly IUserApi _api;

        public UserManagementService() {
            var client = new HttpClient { BaseAddress = new Uri("http://localhost:5297") };
            _api = RestService.For<IUserApi>(client);
        }

        public async Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query) {
            var result = await _api.SearchAsync(query);
            return (result.users, result.total);
        }

        public Task<List<UserRole>> GetRolesAsync() => _api.GetRolesAsync();

        public async Task<bool> AddAsync(UserCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.success;
        }

        public async Task<bool> UpdateAsync(UserEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.success;
        }

        public async Task<bool> DisableAsync(Guid id) {
            var resp = await _api.DisableAsync(id);
            return resp.success;
        }

        public async Task<bool> EnableAsync(Guid id) {
            var resp = await _api.EnableAsync(id);
            return resp.success;
        }
    }
}
