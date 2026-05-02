using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for User endpoints.
/// </summary>
public interface ILocalUserApi
{
    [Refit.Get("/api/users")]
    Task<List<UserListDto>> GetUsersAsync();

    [Refit.Get("/api/users/{id}")]
    Task<UserDetailDto> GetUserByIdAsync(Guid id);

    [Refit.Put("/api/users/{id}")]
    Task<UserDetailDto> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);
}
