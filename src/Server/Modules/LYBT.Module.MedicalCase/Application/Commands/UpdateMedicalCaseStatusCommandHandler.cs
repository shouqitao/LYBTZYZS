using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class UpdateMedicalCaseStatusCommandHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<UpdateMedicalCaseStatusCommand, Result<MedicalCaseDetailDto>>
{
    public async Task<Result<MedicalCaseDetailDto>> Handle(
        UpdateMedicalCaseStatusCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.CaseStatus = request.Status;
        medicalCase.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(medicalCase, cancellationToken);

        var dto = mapper.MapToMedicalCaseDetailDto(medicalCase);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}


