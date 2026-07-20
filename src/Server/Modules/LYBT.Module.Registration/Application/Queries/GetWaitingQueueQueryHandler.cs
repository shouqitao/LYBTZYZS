using LYBT.Module.Registration.Mapping;
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
    private readonly RegistrationMapper _mapper;

    public GetWaitingQueueQueryHandler(IRegistrationRepository repository, RegistrationMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<RegistrationListDto>> Handle(
        GetWaitingQueueQuery request, CancellationToken cancellationToken)
    {
        var entities = await _repository.GetWaitingQueueAsync(request.DoctorId, cancellationToken);
        return entities.Select(x => _mapper.ToListDto(x)).ToList();
    }
}


