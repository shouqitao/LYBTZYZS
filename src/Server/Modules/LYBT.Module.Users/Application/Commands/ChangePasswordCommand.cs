using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Commands;

public record ChangePasswordCommand(
    Guid Id,
    string OldPassword,
    string NewPassword,
    Guid CurrentUserId
) : IRequest<Result>;


