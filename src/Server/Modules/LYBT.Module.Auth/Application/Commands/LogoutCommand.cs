using MediatR;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Auth.Application.Commands;

/// <summary>
/// 用户登出命令。
/// </summary>
public record LogoutCommand(
    LogoutRequest Input
) : IRequest<Result<bool>>;


