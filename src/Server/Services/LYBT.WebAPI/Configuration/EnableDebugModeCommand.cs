using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record EnableDebugModeCommand(
    string? Level,
    int? DurationMinutes,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<object>>;


