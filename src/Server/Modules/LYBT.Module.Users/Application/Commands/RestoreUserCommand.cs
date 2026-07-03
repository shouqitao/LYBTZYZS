using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record RestoreUserCommand(
    Guid UserId,
    Guid OperatorId
) : IRequest<Result<UserDetailDto>>;
