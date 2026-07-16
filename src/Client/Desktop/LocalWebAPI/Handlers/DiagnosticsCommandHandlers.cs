using LYBT.LocalWebAPI.Commands;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;
using Serilog.Events;

namespace LYBT.LocalWebAPI.Handlers;

public class GetLoggingStatusQueryHandler : IRequestHandler<GetLoggingStatusQuery, ApiResponse<object>>
{
    private readonly LoggingLevelManager _loggingLevelManager;

    public GetLoggingStatusQueryHandler(LoggingLevelManager loggingLevelManager)
    {
        _loggingLevelManager = loggingLevelManager;
    }

    public Task<ApiResponse<object>> Handle(GetLoggingStatusQuery query, CancellationToken cancellationToken)
    {
        var status = _loggingLevelManager.GetStatus();
        return Task.FromResult(ApiResponse<object>.CreateSuccess(new
        {
            currentLevel = status.CurrentLevel,
            defaultLevel = status.DefaultLevel,
            isDebugModeActive = status.IsActive,
            debugModeStartedAt = status.StartedAt,
            debugModeExpiresAt = status.ExpiresAt,
            remainingMinutes = status.ExpiresAt.HasValue
                ? Math.Max(0, (int)(status.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes)
                : (int?)null
        }));
    }
}

public class EnableDebugModeCommandHandler : IRequestHandler<EnableDebugModeCommand, ApiResponse<object>>
{
    private readonly LoggingLevelManager _loggingLevelManager;

    public EnableDebugModeCommandHandler(LoggingLevelManager loggingLevelManager)
    {
        _loggingLevelManager = loggingLevelManager;
    }

    public Task<ApiResponse<object>> Handle(EnableDebugModeCommand command, CancellationToken cancellationToken)
    {
        var level = command.Level?.ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        var durationMinutes = command.DurationMinutes ?? 30;
        if (durationMinutes > 120) durationMinutes = 120;

        var result = _loggingLevelManager.EnableDebugMode(level, durationMinutes);

        return Task.FromResult(ApiResponse<object>.CreateSuccess(new
        {
            message = "调试模式已启用",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel,
            startedAt = result.StartedAt,
            expiresAt = result.ExpiresAt,
            durationMinutes = result.DurationMinutes
        }));
    }
}

public class DisableDebugModeCommandHandler : IRequestHandler<DisableDebugModeCommand, ApiResponse<object>>
{
    private readonly LoggingLevelManager _loggingLevelManager;

    public DisableDebugModeCommandHandler(LoggingLevelManager loggingLevelManager)
    {
        _loggingLevelManager = loggingLevelManager;
    }

    public Task<ApiResponse<object>> Handle(DisableDebugModeCommand command, CancellationToken cancellationToken)
    {
        var result = _loggingLevelManager.DisableDebugMode();

        return Task.FromResult(ApiResponse<object>.CreateSuccess(new
        {
            message = "调试模式已禁用，已恢复默认日志级别",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel
        }));
    }
}

public class SetLoggingLevelCommandHandler : IRequestHandler<SetLoggingLevelCommand, ApiResponse<object>>
{
    private readonly LoggingLevelManager _loggingLevelManager;

    public SetLoggingLevelCommandHandler(LoggingLevelManager loggingLevelManager)
    {
        _loggingLevelManager = loggingLevelManager;
    }

    public Task<ApiResponse<object>> Handle(SetLoggingLevelCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<LogEventLevel>(command.Level, ignoreCase: true, out var level))
        {
            return Task.FromResult(ApiResponse<object>.CreateFail(
                $"无效的日志级别，有效值: {string.Join(", ", Enum.GetNames<LogEventLevel>())}"));
        }

        var previousLevel = _loggingLevelManager.GetStatus().CurrentLevel;
        _loggingLevelManager.SetLevel(level);

        return Task.FromResult(ApiResponse<object>.CreateSuccess(new
        {
            message = "日志级别已更新",
            previousLevel,
            currentLevel = level.ToString()
        }));
    }
}
