using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证查询服务接口 - UltraThink双层架构简化版
/// 职责：基础状态查询、简单连接检查
/// </summary>
public interface IAuthQueryService {

    #region 基础认证状态查询

    /// <summary>
    /// 检查是否已登录
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<ServiceResult<UserDto?>> GetCurrentUserAsync();

    /// <summary>
    /// 检查API连接状态
    /// </summary>
    Task<ServiceResult<bool>> CheckConnectionAsync();

    #endregion 基础认证状态查询
}
