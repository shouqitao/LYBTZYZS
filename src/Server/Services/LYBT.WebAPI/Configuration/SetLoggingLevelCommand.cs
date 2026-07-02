using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record SetLoggingLevelCommand(
    string Level,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<object>>;


