using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCasePermissionsQuery(
    Guid CaseId,
    Guid UserId,
    int UserRole
) : IRequest<Result<MedicalCasePermissionsDto>>;
