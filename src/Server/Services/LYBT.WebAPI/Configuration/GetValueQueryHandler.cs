using LYBT.Infrastructure.Configuration.Services;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public class GetValueQueryHandler(
    ISystemConfigurationService configurationService
) : IRequestHandler<GetValueQuery, Result<string?>>
{
    public async Task<Result<string?>> Handle(
        GetValueQuery request, CancellationToken cancellationToken)
    {
        var result = await configurationService.GetValueAsync(request.Key, cancellationToken);
        if (!result.IsSuccess)
            return Result<string?>.Failure(ErrorCode.NotFound, result.ErrorMessage ?? "配置项不存在");

        return Result<string?>.Success(result.Data);
    }
}


