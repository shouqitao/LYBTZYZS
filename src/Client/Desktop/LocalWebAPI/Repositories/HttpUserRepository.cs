using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpUserRepository : IUserRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpUserRepository> _logger;

    public HttpUserRepository(IApiClient apiClient, ILogger<HttpUserRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.GetUsersAsync(page, pageSize, keyword);
        if (response.Data == null)
            return new PagedResult<UserListDto>();
        return new PagedResult<UserListDto>
        {
            Items = response.Data.Items.ToList(),
            TotalCount = response.Data.TotalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.GetUserByIdAsync(id);
        return response.Data;
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto user, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.CreateUserAsync(user);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create user failed");
        return response.Data;
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto user, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.UpdateUserAsync(user.Id!.Value, user);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update user failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.DeleteUserAsync(id);
        return response.Success;
    }

    public async Task<UserDetailDto> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.GetUsersAsync(1, 100, username);
        if (response.Data == null)
            throw new KeyNotFoundException($"User not found: {username}");
        var user = response.Data.Items.FirstOrDefault(u => u.UserName == username);
        if (user == null)
            throw new KeyNotFoundException($"User not found: {username}");
        var detailResponse = await _apiClient.Users.GetUserByIdAsync(user.Id);
        if (detailResponse.Data == null)
            throw new KeyNotFoundException($"User not found: {username}");
        return detailResponse.Data;
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.GetUsersAsync(1, 100, keyword);
        if (response.Data == null)
            return [];
        return response.Data.Items.ToList();
    }

    public async Task<List<UserListDto>> GetDoctorsAsync(CancellationToken ct = default)
    {
        var all = await SearchAsync("", ct);
        return all.Where(u => u.Role == UserRole.Doctor).ToList();
    }

    public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.ChangeProfileAsync(userId, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Change profile failed");
        return response.Data;
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.ChangePasswordAsync(userId, request);
        return response.Success
            ? Result.Success()
            : Result.Failure("修改密码失败");
    }

    public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.ResetPasswordAsync(userId, request);
        if (response.Success && response.Data != null)
            return Result<ResetPasswordResponseDto>.Success(response.Data);
        return Result<ResetPasswordResponseDto>.Failure(response.Message ?? "Reset password failed");
    }

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.ToggleStatusAsync(id);
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        var response = await _apiClient.Users.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

}
