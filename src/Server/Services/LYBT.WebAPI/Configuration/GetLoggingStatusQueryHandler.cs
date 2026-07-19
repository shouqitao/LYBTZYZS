using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public class GetLoggingStatusQueryHandler(
    LoggingLevelManager loggingLevelManager
) : IRequestHandler<GetLoggingStatusQuery, Result<object>>
{
    public Task<Result<object>> Handle(
        GetLoggingStatusQuery request, CancellationToken cancellationToken)
    {
        var status = loggingLevelManager.GetStatus();
        object result = new
        {
            currentLevel = status.CurrentLevel,
            defaultLevel = status.DefaultLevel,
            isDebugModeActive = status.IsActive,
            debugModeStartedAt = status.StartedAt,
            debugModeExpiresAt = status.ExpiresAt,
            remainingMinutes = status.ExpiresAt.HasValue
                ? Math.Max(0, (int)(status.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes)
                : (int?)null
        };
        return Task.FromResult(Result<object>.Success(result));
    }
}


