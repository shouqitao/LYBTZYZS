using MediatR;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Auth.Application.Commands;

/// <summary>
/// 刷新令牌命令。
/// </summary>
public record RefreshTokenCommand(
    string Token
) : IRequest<Result<LoginResponse>>;


