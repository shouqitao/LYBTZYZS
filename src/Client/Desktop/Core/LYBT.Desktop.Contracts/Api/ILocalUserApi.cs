using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for User endpoints.
/// </summary>
public interface ILocalUserApi
{
    [Refit.Get("/api/v1/users")]
    Task<List<UserListDto>> GetUsersAsync();

    [Refit.Get("/api/v1/users/{id}")]
    Task<UserDetailDto> GetUserByIdAsync(Guid id);

    [Refit.Post("/api/v1/users")]
    Task<UserDetailDto> CreateUserAsync([Refit.Body] UserInputDto request);

    [Refit.Put("/api/v1/users/{id}")]
    Task<UserDetailDto> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);

    [Refit.Delete("/api/v1/users/{id}")]
    Task DeleteUserAsync(Guid id);

    [Refit.Put("/api/v1/users/{id}/change-password")]
    Task ChangePasswordAsync(Guid id, [Refit.Body] ChangePasswordRequest request);

    [Refit.Post("/api/v1/users/{id}/reset-password")]
    Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, [Refit.Body] ResetPasswordRequestDto request);

    [Refit.Put("/api/v1/users/{id}/profile")]
    Task<UserDetailDto> ChangeProfileAsync(Guid id, [Refit.Body] ChangeProfileDto request);

    [Refit.Post("/api/v1/users/{id}/toggle-status")]
    Task<UserDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/v1/users/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Get("/api/v1/users/current")]
    Task<UserDetailDto> GetCurrentUserAsync();
}
