using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 接诊请求。从等待队列选中患者开始接诊。
/// </summary>
public sealed record StartVisitCommand(Guid RegistrationId) : IRequest<Guid>;


