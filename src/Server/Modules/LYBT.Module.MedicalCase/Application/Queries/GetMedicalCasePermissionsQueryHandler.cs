using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCasePermissionsQueryHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<GetMedicalCasePermissionsQuery, Result<MedicalCasePermissionsDto>>
{
    public async Task<Result<MedicalCasePermissionsDto>> Handle(
        GetMedicalCasePermissionsQuery request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.CaseId, cancellationToken);
        if (medicalCase == null)
            return Result<MedicalCasePermissionsDto>.Failure(ErrorCode.NotFound, "医案不存在");

        var isOwner = medicalCase.UserId == request.UserId;
        var isAdmin = request.UserRole == (int)UserRole.Admin || request.UserRole == (int)UserRole.SuperAdmin;
        var status = medicalCase.CaseStatus;

        var dto = new MedicalCasePermissionsDto
        {
            CanEdit = (status == MedicalCaseStatus.Active || status == MedicalCaseStatus.Suspended) && (isOwner || isAdmin),
            CanComplete = status == MedicalCaseStatus.Active && isOwner,
            CanSuspend = (status == MedicalCaseStatus.Active || status == MedicalCaseStatus.Suspended) && isOwner,
            CanCancel = status == MedicalCaseStatus.Active && isOwner,
            CanDelete = !medicalCase.IsDeleted && (isOwner || isAdmin)
        };

        return Result<MedicalCasePermissionsDto>.Success(dto);
    }
}
