using MediatR;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.LocalWebAPI.Commands;

public record LocalAutoLoginCommand(AutoLoginRequest Request) : IRequest<ApiResponse<LoginResponse>>;
