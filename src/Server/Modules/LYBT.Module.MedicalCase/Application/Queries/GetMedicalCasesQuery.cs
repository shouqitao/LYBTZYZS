using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCasesQuery(
    MedicalCaseStatus? Status = null,
    Guid? PatientId = null,
    int Page = 1,
    int PageSize = 20,
    Guid? CurrentDoctorId = null,
    bool IsAdmin = false,
    string? Keyword = null
) : IRequest<Result<PagedResult<MedicalCaseListDto>>>;


