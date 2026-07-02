using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;

namespace LYBT.Module.Registration.Application.Queries;

/// <summary>
/// 分页查询挂号记录请求。
/// </summary>
public sealed record GetRegistrationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? PatientId = null,
    Guid? DoctorId = null) : IRequest<PagedResult<RegistrationListDto>>;


