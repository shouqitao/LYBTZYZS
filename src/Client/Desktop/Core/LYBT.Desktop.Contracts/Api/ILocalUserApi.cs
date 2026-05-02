using LYBT.Shared.Models.Contracts.Auth;
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

    [Refit.Post("/api/users")]
    Task<UserDetailDto> CreateUserAsync([Refit.Body] UserInputDto request);

    [Refit.Put("/api/users/{id}")]
    Task<UserDetailDto> UpdateUserAsync(Guid id, [Refit.Body] UserInputDto request);

    [Refit.Delete("/api/users/{id}")]
    Task DeleteUserAsync(Guid id);

    [Refit.Put("/api/users/{id}/change-password")]
    Task ChangePasswordAsync(Guid id, [Refit.Body] ChangePasswordRequest request);

    [Refit.Post("/api/users/{id}/reset-password")]
    Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id);

    [Refit.Put("/api/users/{id}/profile")]
    Task<UserDetailDto> ChangeProfileAsync(Guid id, [Refit.Body] ChangeProfileDto request);

    [Refit.Post("/api/users/{id}/toggle-status")]
    Task<UserDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/users/{id}/restore")]
    Task<UserDetailDto> RestoreAsync(Guid id);

    [Refit.Post("/api/users/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/users/batch-enable")]
    Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/users/batch-disable")]
    Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

    [Refit.Get("/api/users/current")]
    Task<UserDetailDto> GetCurrentUserAsync();
}
