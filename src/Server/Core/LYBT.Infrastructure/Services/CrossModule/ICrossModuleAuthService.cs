namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 认证域跨模块服务 (AUTH-D06/D07)
/// 供 Users 模块在角色变更/禁用等场景触发 Token 撤销
/// </summary>
public interface ICrossModuleAuthService
{
    /// <summary>撤销指定用户的所有 Token Family</summary>
    Task RevokeUserTokensAsync(Guid userId, string reason);
}
