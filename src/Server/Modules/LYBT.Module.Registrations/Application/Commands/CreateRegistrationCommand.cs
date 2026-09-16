using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

/// <summary>
/// 创建挂号请求。
/// </summary>
public sealed record CreateRegistrationCommand(RegistrationInputDto Input, Guid OperatorId) : IRequest<Result<RegistrationDetailDto>>;


