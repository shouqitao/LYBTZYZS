using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Auth.Application.Commands;

/// <summary>
/// 撤销用户全部令牌命令（Token 族旋转）。
/// </summary>
public record RevokeAllUserTokensCommand(
    Guid UserId,
    string Reason
) : IRequest<Result<bool>>;
