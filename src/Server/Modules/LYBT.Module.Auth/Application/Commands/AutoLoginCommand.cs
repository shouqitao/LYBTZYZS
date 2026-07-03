using MediatR;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Application.Commands;

public record AutoLoginCommand(
    string Token,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<LoginResponse>>;
