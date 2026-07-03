using MediatR;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Commands;

public class AutoLoginCommandHandler : IRequestHandler<AutoLoginCommand, Result<LoginResponse>>
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<AutoLoginCommandHandler> _logger;

    public AutoLoginCommandHandler(
        IJwtService jwtService,
        ILogger<AutoLoginCommandHandler> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public Task<Result<LoginResponse>> Handle(
        AutoLoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Handler] AutoLogin - TokenLength={TokenLength} IpAddress={IpAddress}",
            request.Token?.Length ?? 0, request.IpAddress ?? "unknown");

        var result = _jwtService.ValidateAutoLoginToken(request.Token);
        return Task.FromResult(result);
    }
}
