using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Services;

/// <summary>
/// 用户查询服务 - UltraThink双层架构查询专业层（精简版）
/// 职责：用户信息查询、搜索过滤（仅保留核心功能）
/// </summary>
public class UserQueryService(
    ILogger<UserQueryService> logger,
    IUserApi userApi) : IUserQueryService
{
    private readonly ILogger<UserQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserApi _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));

    #region 核心查询操作

    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
    {
        try
        {
            _logger.LogDebug(
                "执行用户分页查询，页码: {PageIndex}, 页大小: {PageSize}",
                query.PageIndex, query.PageSize);

            var refitResponse = await _userApi.GetUsersAsync(
                page: query.PageIndex,
                pageSize: query.PageSize,
                keyword: query.Keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    return ServiceResult<PagedResult<UserDto>>.Success(apiResponse.Data);
                }

                return ServiceResult<PagedResult<UserDto>>.Failure(apiResponse.Message ?? "查询用户列表失败");
            }

            return ServiceResult<PagedResult<UserDto>>.Failure("查询用户网络请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户分页查询异常");
            return ServiceResult<PagedResult<UserDto>>.Failure("查询用户列表失败");
        }
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("查询用户详情: {UserId}", id);

            var refitResponse = await _userApi.GetUserByIdAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    return ServiceResult<UserDto>.Success(apiResponse.Data);
                }

                return ServiceResult<UserDto>.Failure(apiResponse.Message ?? "用户不存在");
            }

            return ServiceResult<UserDto>.Failure("查询用户网络请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询用户详情异常: {UserId}", id);
            return ServiceResult<UserDto>.Failure("查询用户详情失败");
        }
    }

    public Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
    {
        _logger.LogWarning("简单诊所版本暂不支持按用户名查询功能: {Username}", username);
        return Task.FromResult(ServiceResult<UserDto>.Failure("简单诊所版本暂不支持按用户名查询"));
    }

    public Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
    {
        _logger.LogWarning("简单诊所版本暂不支持用户搜索功能: {Keyword}", keyword);
        return Task.FromResult(ServiceResult<List<UserDto>>.Failure("简单诊所版本暂不支持用户搜索"));
    }

    public Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
    {
        _logger.LogWarning("简单诊所版本暂不支持获取启用用户列表功能");
        return Task.FromResult(ServiceResult<List<UserDto>>.Failure("简单诊所版本暂不支持用户状态查询"));
    }

    public Task<ServiceResult<List<object>>> GetRolesAsync()
    {
        _logger.LogDebug("获取系统角色列表");

        // 简单诊所版本固定角色
        var roles = new List<object>
        {
            new { Id = "Admin", Name = "管理员" },
            new { Id = "Doctor", Name = "医生" }
        };

        return Task.FromResult(ServiceResult<List<object>>.Success(roles));
    }

    public Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
    {
        _logger.LogWarning("简单诊所版本暂不支持用户名验证功能: {Username}", username);
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持用户名验证"));
    }

    #endregion 核心查询操作
}
