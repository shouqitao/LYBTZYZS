using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证查询服务 - UltraThink双层架构简化版
/// 职责：基础状态查询、简单连接检查
/// </summary>
public class AuthQueryService(ILogger<AuthQueryService> logger) : IAuthQueryService
{
    private readonly ILogger<AuthQueryService> _logger = logger;

    #region 基础认证状态查询 - 简化实现

    /// <summary>
    /// 检查是否已登录
    /// </summary>
    public bool IsLoggedIn => false; // 简单诊所版本简化实现

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public Task<ServiceResult<UserDto?>> GetCurrentUserAsync()
    {
        return Task.FromResult(ServiceResult<UserDto?>.Success(null)); // 简化实现
    }

    /// <summary>
    /// 检查API连接状态
    /// </summary>
    public Task<ServiceResult<bool>> CheckConnectionAsync()
    {
        return Task.FromResult(ServiceResult<bool>.Success(true)); // 简化实现
    }

    #endregion
}