using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Interfaces;

/// <summary>
/// Token 管理服务接口 - 负责 Token 刷新、验证、会话查询和 Family 撤销
/// 从 IAuthService 拆分而来，职责: Token 生命周期管理
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// 刷新 Token，包含 Token 轮换和重放攻击检测
    /// </summary>
    Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 验证 JWT Token 有效性
    /// </summary>
    Task<Result<bool>> ValidateTokenAsync(string token);

    /// <summary>
    /// 从 Token 提取会话信息 (UserId/UserName/Role)
    /// </summary>
    Task<Result<object>> GetSessionInfoAsync(string token);

    /// <summary>
    /// 撤销整个 RefreshToken Family (用于重放攻击检测和登出)
    /// </summary>
    Task RevokeTokenFamilyAsync(string familyId, string reason);
}
