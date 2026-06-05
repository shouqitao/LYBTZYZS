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

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
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

    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _apiClient.Users.GetUserByIdAsync(id);
        return response.Data;
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto user)
    {
        var response = await _apiClient.Users.CreateUserAsync(user);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create user failed");
        return response.Data;
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto user)
    {
        var response = await _apiClient.Users.UpdateUserAsync(user.Id!.Value, user);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update user failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _apiClient.Users.DeleteUserAsync(id);
        return response.Success;
    }

    public async Task<UserDetailDto> GetByUsernameAsync(string username)
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

    public async Task<List<UserListDto>> SearchAsync(string keyword)
    {
        var response = await _apiClient.Users.GetUsersAsync(1, 100, keyword);
        if (response.Data == null)
            return [];
        return response.Data.Items.ToList();
    }

    public async Task<List<UserListDto>> GetDoctorsAsync()
    {
        var all = await SearchAsync("");
        return all.Where(u => u.Role == UserRole.Doctor).ToList();
    }

    public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
    {
        var response = await _apiClient.Users.ChangeProfileAsync(userId, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Change profile failed");
        return response.Data;
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var response = await _apiClient.Users.ChangePasswordAsync(userId, request);
        return response.Success
            ? new ServiceResult { IsSuccess = true }
            : new ServiceResult { IsSuccess = false };
    }

    public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request)
    {
        var response = await _apiClient.Users.ResetPasswordAsync(userId, request);
        if (response.Success && response.Data != null)
            return ServiceResult<ResetPasswordResponseDto>.Success(response.Data);
        return ServiceResult<ResetPasswordResponseDto>.Failure(response.Message ?? "Reset password failed");
    }

    public async Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
    {
        var response = await _apiClient.Users.BatchImportAsync(request);
        return response.Data;
    }

    public async Task<UserDetailDto?> ToggleStatusAsync(Guid id)
    {
        var response = await _apiClient.Users.ToggleStatusAsync(id);
        return response.Data;
    }

    public async Task<UserDetailDto?> RestoreAsync(Guid id)
    {
        var response = await _apiClient.Users.RestoreAsync(id);
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var response = await _apiClient.Users.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Users.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Users.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }
}
