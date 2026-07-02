using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 取消挂号请求。
/// </summary>
public sealed record CancelRegistrationCommand(Guid RegistrationId) : IRequest;


