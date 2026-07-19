using LYBT.Infrastructure.Configuration.Services;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.WebAPI.Configuration.Commands;

public class ValidateConfigurationQueryHandler(
    ISystemConfigurationService configurationService
) : IRequestHandler<ValidateConfigurationQuery, Result>
{
    public async Task<Result> Handle(
        ValidateConfigurationQuery request, CancellationToken cancellationToken)
    {
        var result = await configurationService.ValidateProductionConfigAsync(cancellationToken);
        if (!result.IsSuccess)
            return Result.Failure(ErrorCode.InvalidRequest, result.ErrorMessage ?? "配置验证失败");

        return Result.Success();
    }
}


