using LYBT.Module.Registration.Application.Mappers;
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

    public GetRegistrationQueryHandler(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<RegistrationDetailDto?> Handle(
        GetRegistrationQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null) return null;

        return RegistrationMapper.ToDetailDto(entity);
    }
}


