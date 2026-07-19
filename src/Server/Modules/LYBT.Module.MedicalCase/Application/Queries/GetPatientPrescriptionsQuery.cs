using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetPatientPrescriptionsQuery(
    Guid PatientId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<PrescriptionDetailDto>>>;
