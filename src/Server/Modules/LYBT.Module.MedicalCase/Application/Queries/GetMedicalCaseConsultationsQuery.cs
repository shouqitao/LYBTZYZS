using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCaseConsultationsQuery(
    Guid MedicalCaseId
) : IRequest<Result<List<ConsultationDetailDto>>>;


