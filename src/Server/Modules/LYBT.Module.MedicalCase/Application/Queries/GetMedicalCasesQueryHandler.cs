using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCasesQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetMedicalCasesQuery, Result<PagedResult<MedicalCaseListDto>>>
{
    public async Task<Result<PagedResult<MedicalCaseListDto>>> Handle(
        GetMedicalCasesQuery request, CancellationToken cancellationToken)
    {
        var result = await repository.GetPagedWithDetailsAsync(
            request.Page, request.PageSize,
            request.Status, request.PatientId, request.CurrentDoctorId,
            request.IsAdmin, request.Keyword, cancellationToken);

        var dtos = mapper.ToListDtos(result.Items.ToList());

        for (int i = 0; i < result.Items.Count && i < dtos.Count; i++)
        {
            var entity = result.Items[i];
            dtos[i].HasConsultation = entity.Consultation != null && !entity.Consultation.IsDeleted;
            dtos[i].HasPrescription = entity.Prescription != null && !entity.Prescription.IsDeleted;
        }

        return Result<PagedResult<MedicalCaseListDto>>.Success(new PagedResult<MedicalCaseListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = request.Page,
            PageSize = request.PageSize
        });
    }
}


