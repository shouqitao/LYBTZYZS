using LYBT.Entities.Users;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for User endpoints.
/// </summary>
public interface ILocalUserApi
{
    [Refit.Get("/api/users")]
    Task<List<User>> GetUsersAsync();

    [Refit.Get("/api/users/{id}")]
    Task<User> GetUserByIdAsync(Guid id);

    [Refit.Put("/api/users/{id}")]
    Task<User> UpdateUserAsync(Guid id, [Refit.Body] User user);
}
