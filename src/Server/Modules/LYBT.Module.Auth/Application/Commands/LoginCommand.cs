using MediatR;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Auth.Application.Commands;

/// <summary>
/// 用户登录命令。
/// </summary>
public record LoginCommand(
    LoginRequest Input
) : IRequest<Result<LoginResponse>>;


