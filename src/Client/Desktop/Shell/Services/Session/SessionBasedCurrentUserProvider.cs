using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Shell.Services.Session;

/// <summary>
/// 基于会话的当前用户提供者
/// OpenSpec: implement-local-mode
/// 从 SessionManager 获取当前用户 ID，供 LocalDbContext 使用
/// </summary>
public class SessionBasedCurrentUserProvider : ICurrentUserProvider
{
    private readonly ISessionManager _sessionManager;

    public SessionBasedCurrentUserProvider(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <inheritdoc />
    public Guid? CurrentUserId => _sessionManager.CurrentUserId;
}
