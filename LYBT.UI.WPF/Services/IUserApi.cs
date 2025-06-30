using LYBT.Module.Users.Dtos;
using LYBT.Common.Enums.Users;
using Refit;

namespace LYBT.UI.WPF.Services {
    public class UserSearchResponseDto {
        public int total { get; set; }
        public List<UserDto> users { get; set; } = new();
    }

    public class SuccessResponseDto {
        public bool success { get; set; }
        public int count { get; set; }
    }

    public interface IUserApi {
        [Get("/api/Users/search")]
        Task<UserSearchResponseDto> SearchAsync([Query] UserQueryDto query);

        [Post("/api/Users/add")]
        Task<SuccessResponseDto> AddAsync([Body] UserCreateDto dto);

        [Put("/api/Users/update")]
        Task<SuccessResponseDto> UpdateAsync([Body] UserEditDto dto);

        [Post("/api/Users/disable/{id}")]
        Task<SuccessResponseDto> DisableAsync(Guid id);

        [Post("/api/Users/enable/{id}")]
        Task<SuccessResponseDto> EnableAsync(Guid id);

        [Get("/api/Users/getRoles")]
        Task<List<UserRole>> GetRolesAsync();
    }
}
