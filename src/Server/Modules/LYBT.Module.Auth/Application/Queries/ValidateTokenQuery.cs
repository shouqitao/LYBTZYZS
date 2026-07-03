using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Auth.Application.Queries;

/// <summary>
/// 验证令牌有效性查询。
/// </summary>
public record ValidateTokenQuery(
    string Token
) : IRequest<Result<ValidateTokenResult>>;


