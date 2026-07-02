using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCaseConsultationsQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetMedicalCaseConsultationsQuery, Result<List<ConsultationDetailDto>>>
{
    public async Task<Result<List<ConsultationDetailDto>>> Handle(
        GetMedicalCaseConsultationsQuery request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.MedicalCaseId, cancellationToken);
        if (medicalCase?.Consultation == null)
            return Result<List<ConsultationDetailDto>>.Success(new List<ConsultationDetailDto>());

        var dto = mapper.ToConsultationDetailDto(medicalCase.Consultation);
        return Result<List<ConsultationDetailDto>>.Success(new List<ConsultationDetailDto> { dto });
    }
}


