using MediatR;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Auth.Application.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IJwtService jwtService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    public Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = _jwtService.RefreshToken(request.Token);
        if (result.IsSuccess)
        {
            _logger.LogInformation("[Handler] Token refreshed successfully");
            return Task.FromResult(Result<LoginResponse>.Success(result.Data!));
        }

        _logger.LogWarning("[Handler] Token refresh failed - {Error}", result.ErrorMessage);
        return Task.FromResult(Result<LoginResponse>.Failure(
            result.ModuleErrorCode ?? ErrorCode.AuthInvalidCredentials,
            result.ErrorMessage ?? "令牌刷新失败"));
    }
}


