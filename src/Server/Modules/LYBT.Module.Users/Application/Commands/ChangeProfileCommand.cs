using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Commands;

public record ChangeProfileCommand(
    Guid Id,
    ChangeProfileDto Dto,
    Guid CurrentUserId
) : IRequest<Result<UserDetailDto>>;


