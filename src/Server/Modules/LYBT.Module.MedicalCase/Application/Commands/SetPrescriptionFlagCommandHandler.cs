using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class SetPrescriptionFlagCommandHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<SetPrescriptionFlagCommand, Result<MedicalCaseDetailDto>>
{
    public async Task<Result<MedicalCaseDetailDto>> Handle(
        SetPrescriptionFlagCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.NeedsPrescription = request.NeedsPrescription;
        medicalCase.UpdatedAt = DateTime.UtcNow;

        if (!request.NeedsPrescription && medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
        {
            medicalCase.Prescription.IsDeleted = true;
            medicalCase.Prescription.UpdatedAt = DateTime.UtcNow;
        }

        await repository.UpdateAsync(medicalCase, cancellationToken);

        var dto = mapper.MapToMedicalCaseDetailDto(medicalCase);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}


