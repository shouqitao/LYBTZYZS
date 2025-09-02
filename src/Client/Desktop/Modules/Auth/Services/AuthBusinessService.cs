using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Auth.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Auth.Services;

/// <summary>
/// 认证业务服务 - UltraThink双层架构简化版
/// 职责：基础认证操作
/// </summary>
public class AuthBusinessService(ILogger<AuthBusinessService> logger) : IAuthBusinessService
{
    private readonly ILogger<AuthBusinessService> _logger = logger;

    #region 基础认证流程 - 简化实现

    /// <summary>
    /// 用户登录
    /// </summary>
    public Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        return Task.FromResult(ServiceResult<LoginResponse>.Failure("简单诊所版本暂不支持登录功能"));
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public Task<ServiceResult> LogoutAsync()
    {
        return Task.FromResult(ServiceResult.Failure("简单诊所版本暂不支持注销功能"));
    }

    /// <summary>
    /// Token刷新
    /// </summary>
    public Task<ServiceResult<LoginResponse>> RefreshTokenAsync()
    {
        return Task.FromResult(ServiceResult<LoginResponse>.Failure("简单诊所版本暂不支持令牌刷新"));
    }

    #endregion
}