using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetPatientConsultationsQuery(
    Guid PatientId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<ConsultationDetailDto>>>;
