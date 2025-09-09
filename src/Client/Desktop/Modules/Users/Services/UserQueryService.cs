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

    public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
    {
        try
        {
            _logger.LogDebug("按用户名查询用户: {Username}", username);

            // 使用GetUsersAsync API按用户名搜索
            var refitResponse = await _userApi.GetUsersAsync(
                page: 1,
                pageSize: 1,
                username: username);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null && apiResponse.Data.Items.Any())
                {
                    var user = apiResponse.Data.Items.First();
                    return ServiceResult<UserDto>.Success(user, "用户查询成功");
                }

                return ServiceResult<UserDto>.Failure("未找到指定用户名的用户");
            }

            return ServiceResult<UserDto>.Failure("按用户名查询网络请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按用户名查询用户异常: {Username}", username);
            return ServiceResult<UserDto>.Failure("按用户名查询用户失败");
        }
    }

    public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("搜索用户: {Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<UserDto>>.Success([]);
            }

            // 使用GetUsersAsync API进行关键字搜索
            var refitResponse = await _userApi.GetUsersAsync(
                page: 1,
                pageSize: 100, // 搜索结果限制为100条
                keyword: keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    var users = apiResponse.Data.Items.ToList();
                    _logger.LogDebug("用户搜索成功: {Keyword}, 结果数: {Count}", keyword, users.Count);
                    return ServiceResult<List<UserDto>>.Success(users, "搜索成功");
                }

                return ServiceResult<List<UserDto>>.Failure(apiResponse.Message ?? "用户搜索失败");
            }

            return ServiceResult<List<UserDto>>.Success([], "搜索网络请求失败，返回空结果");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户搜索异常: {Keyword}", keyword);
            return ServiceResult<List<UserDto>>.Failure($"用户搜索失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
    {
        try
        {
            _logger.LogDebug("获取活跃用户列表");

            var refitResponse = await _userApi.GetActiveUsersAsync();

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var apiResponse = refitResponse.Content;
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    var activeUsers = apiResponse.Data.ToList();
                    _logger.LogDebug("获取活跃用户列表成功，用户数: {Count}", activeUsers.Count);
                    return ServiceResult<List<UserDto>>.Success(activeUsers, "获取活跃用户列表成功");
                }

                return ServiceResult<List<UserDto>>.Failure(apiResponse.Message ?? "获取活跃用户列表失败");
            }

            return ServiceResult<List<UserDto>>.Failure("获取活跃用户列表网络请求失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活跃用户列表异常");
            return ServiceResult<List<UserDto>>.Failure("获取活跃用户列表失败");
        }
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

    public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
    {
        try
        {
            _logger.LogDebug("验证用户名可用性: {Username}", username);

            if (string.IsNullOrWhiteSpace(username))
            {
                return ServiceResult<bool>.Failure("用户名不能为空");
            }

            // 通过查询用户名来验证是否已存在
            var userResult = await GetByUsernameAsync(username);

            if (userResult.IsSuccess && userResult.Data != null)
            {
                // 用户名已存在
                return ServiceResult<bool>.Success(false, "用户名已存在");
            }

            if (userResult.ErrorMessage?.Contains("未找到") == true)
            {
                // 用户名可用
                return ServiceResult<bool>.Success(true, "用户名可用");
            }

            // 查询过程中出现其他错误
            return ServiceResult<bool>.Failure(userResult.ErrorMessage ?? "用户名验证失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户名验证异常: {Username}", username);
            return ServiceResult<bool>.Failure("用户名验证失败");
        }
    }

    #endregion 核心查询操作
}
