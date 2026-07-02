using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registration.Application.Queries;

/// <summary>
/// 获取等待队列请求。
/// </summary>
public sealed record GetWaitingQueueQuery(Guid? DoctorId = null)
    : IRequest<List<RegistrationListDto>>;


