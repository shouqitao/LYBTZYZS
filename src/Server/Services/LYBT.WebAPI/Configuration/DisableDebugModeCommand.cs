using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public record DisableDebugModeCommand(
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<object>>;


