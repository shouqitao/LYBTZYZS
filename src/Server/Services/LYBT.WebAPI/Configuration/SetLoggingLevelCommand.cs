using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record SetLoggingLevelCommand(
    string Level,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<object>>;


