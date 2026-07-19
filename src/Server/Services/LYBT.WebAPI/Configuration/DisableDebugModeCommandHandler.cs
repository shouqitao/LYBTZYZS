using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public class DisableDebugModeCommandHandler(
    LoggingLevelManager loggingLevelManager
) : IRequestHandler<DisableDebugModeCommand, Result<object>>
{
    public Task<Result<object>> Handle(
        DisableDebugModeCommand request, CancellationToken cancellationToken)
    {
        var result = loggingLevelManager.DisableDebugMode();

        object response = new
        {
            message = "调试模式已禁用，已恢复默认日志级别",
            previousLevel = result.PreviousLevel,
            currentLevel = result.CurrentLevel
        };
        return Task.FromResult(Result<object>.Success(response));
    }
}


