using LYBT.Module.Registration.Application.Mappers;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registration.Application.Queries;

/// <summary>
/// 获取等待队列处理器。
/// </summary>
public sealed class GetWaitingQueueQueryHandler
    : IRequestHandler<GetWaitingQueueQuery, List<RegistrationListDto>>
{
    private readonly IRegistrationRepository _repository;

    public GetWaitingQueueQueryHandler(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<RegistrationListDto>> Handle(
        GetWaitingQueueQuery request, CancellationToken cancellationToken)
    {
        var entities = await _repository.GetWaitingQueueAsync(request.DoctorId, cancellationToken);
        return entities.Select(RegistrationMapper.ToListDto).ToList();
    }
}


