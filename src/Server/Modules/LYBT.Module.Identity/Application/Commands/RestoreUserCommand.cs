using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Identity.Application.Commands;

public record RestoreUserCommand(
    Guid UserId,
    Guid OperatorId,
    UserRole OperatorRole
) : IRequest<Result<UserDetailDto>>;
