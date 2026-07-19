using LYBT.Shared.Logging.Management;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;
using Serilog.Events;

namespace LYBT.WebAPI.Configuration.Commands;

public class SetLoggingLevelCommandHandler(
    LoggingLevelManager loggingLevelManager
) : IRequestHandler<SetLoggingLevelCommand, Result<object>>
{
    public Task<Result<object>> Handle(
        SetLoggingLevelCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Level))
            return Task.FromResult(Result<object>.Failure(ErrorCode.ValidationFailed, "日志级别不能为空"));

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
            return Task.FromResult(Result<object>.Failure(ErrorCode.ValidationFailed,
                $"无效的日志级别，有效值: {string.Join(", ", Enum.GetNames<LogEventLevel>())}"));

        var previousLevel = loggingLevelManager.GetStatus().CurrentLevel;
        loggingLevelManager.SetLevel(level);

        object response = new
        {
            message = "日志级别已更新",
            previousLevel,
            currentLevel = level.ToString()
        };
        return Task.FromResult(Result<object>.Success(response));
    }
}


