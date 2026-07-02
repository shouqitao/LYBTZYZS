using MediatR;
using LYBT.Module.Auth.Interfaces;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Queries;

public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, Result<bool>>
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<ValidateTokenQueryHandler> _logger;

    public ValidateTokenQueryHandler(
        IJwtService jwtService,
        ILogger<ValidateTokenQueryHandler> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public Task<Result<bool>> Handle(
        ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        var principal = _jwtService.ValidateToken(request.Token);
        var isValid = principal != null;

        _logger.LogDebug("[Handler] Token validation - Valid={Valid}", isValid);
        return Task.FromResult(Result<bool>.Success(isValid));
    }
}


