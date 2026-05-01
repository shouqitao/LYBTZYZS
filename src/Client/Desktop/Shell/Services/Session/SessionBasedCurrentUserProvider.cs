using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.Shell.Services.Session;

public class SessionBasedCurrentUserProvider : ICurrentUserProvider
{
    private readonly ISessionManager _sessionManager;

    public SessionBasedCurrentUserProvider(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    public Guid? CurrentUserId => _sessionManager.CurrentUserId;
}
