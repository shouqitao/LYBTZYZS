using LYBT.Module.Registration.Application.Mappers;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registration.Application.Queries;

/// <summary>
/// 分页查询挂号记录处理器。
/// </summary>
public sealed class GetRegistrationsQueryHandler
    : IRequestHandler<GetRegistrationsQuery, PagedResult<RegistrationListDto>>
{
    private readonly IRegistrationRepository _repository;

    public GetRegistrationsQueryHandler(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<RegistrationListDto>> Handle(
        GetRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await _repository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword,
            request.StartDate, request.EndDate,
            request.PatientId, request.DoctorId, cancellationToken);

        var items = pagedResult.Items
            .Select(RegistrationMapper.ToListDto)
            .ToList();

        return new PagedResult<RegistrationListDto>
        {
            Items = items,
            TotalCount = pagedResult.TotalCount,
            CurrentPage = pagedResult.CurrentPage,
            PageSize = pagedResult.PageSize
        };
    }
}


