using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCasePrescriptionsQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetMedicalCasePrescriptionsQuery, Result<List<PrescriptionDetailDto>>>
{
    public async Task<Result<List<PrescriptionDetailDto>>> Handle(
        GetMedicalCasePrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.MedicalCaseId, cancellationToken);
        if (medicalCase?.Prescription == null)
            return Result<List<PrescriptionDetailDto>>.Success(new List<PrescriptionDetailDto>());

        var dto = mapper.ToPrescriptionDetailDto(medicalCase.Prescription);
        return Result<List<PrescriptionDetailDto>>.Success(new List<PrescriptionDetailDto> { dto });
    }
}


