using LYBT.Module.Registration.Mapping;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registration.Application.Queries;

/// <summary>
/// 获取挂号详情处理器。
/// </summary>
public sealed class GetRegistrationQueryHandler
    : IRequestHandler<GetRegistrationQuery, RegistrationDetailDto?>
{
    private readonly IRegistrationRepository _repository;
    private readonly RegistrationMapper _mapper;

    public GetRegistrationQueryHandler(IRegistrationRepository repository, RegistrationMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<RegistrationDetailDto?> Handle(
        GetRegistrationQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return null;

        return _mapper.ToDetailDto(entity);
    }
}


