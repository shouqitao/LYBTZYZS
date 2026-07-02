using LYBT.Shared.Logging.Management;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using MediatR;
using Serilog.Events;

namespace LYBT.WebAPI.Configuration.Commands;

public class EnableDebugModeCommandHandler(
    LoggingLevelManager loggingLevelManager
) : IRequestHandler<EnableDebugModeCommand, Result<object>>
{
    public Task<Result<object>> Handle(
        EnableDebugModeCommand request, CancellationToken cancellationToken)
    {
        var level = request.Level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        var durationMinutes = request.DurationMinutes ?? 30;
        if (durationMinutes > 120) durationMinutes = 120;

        var result = loggingLevelManager.EnableDebugMode(level, durationMinutes);

        object response = new
        {
            message = "调试模式已启用",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel,
            startedAt = result.StartedAt,
            expiresAt = result.ExpiresAt,
            durationMinutes = result.DurationMinutes
        };
        return Task.FromResult(Result<object>.Success(response));
    }
}


