using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Auth.Interfaces;

/// <summary>
/// 认证业务服务接口 - UltraThink双层架构简化版
/// 职责：基础认证操作
/// </summary>
public interface IAuthBusinessService
{
    #region 基础认证流程

    /// <summary>
    /// 用户登录
    /// </summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task<ServiceResult> LogoutAsync();

    /// <summary>
    /// Token刷新
    /// </summary>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync();

    /// <summary>
    /// 修改系统管理员密码
    /// </summary>
    Task<ServiceResult> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request);

    #endregion
}
