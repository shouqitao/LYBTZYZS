using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCasePrescriptionsQuery(
    Guid MedicalCaseId
) : IRequest<Result<List<PrescriptionDetailDto>>>;


