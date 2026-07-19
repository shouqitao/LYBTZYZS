using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Commands;

public record RestoreUserCommand(
    Guid UserId,
    Guid OperatorId
) : IRequest<Result<UserDetailDto>>;
