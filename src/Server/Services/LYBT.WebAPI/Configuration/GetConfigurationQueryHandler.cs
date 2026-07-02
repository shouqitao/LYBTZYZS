using LYBT.Infrastructure.Configuration.Services;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public class GetConfigurationQueryHandler(
    ISystemConfigurationService configurationService
) : IRequestHandler<GetConfigurationQuery, Result<Dictionary<string, string>>>
{
    public async Task<Result<Dictionary<string, string>>> Handle(
        GetConfigurationQuery request, CancellationToken cancellationToken)
    {
        var result = await configurationService.GetConfigurationAsync(cancellationToken);
        if (!result.IsSuccess)
            return Result<Dictionary<string, string>>.Failure(ErrorCode.Unknown, result.ErrorMessage ?? "获取配置失败");

        return Result<Dictionary<string, string>>.Success(result.Data!);
    }
}


