using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record ToggleUserStatusCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<UserDetailDto>>;


