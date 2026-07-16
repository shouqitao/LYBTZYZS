using MediatR;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.LocalWebAPI.Commands;

public record LocalLoginCommand(LoginRequest Request) : IRequest<ApiResponse<LoginResponse>>;
