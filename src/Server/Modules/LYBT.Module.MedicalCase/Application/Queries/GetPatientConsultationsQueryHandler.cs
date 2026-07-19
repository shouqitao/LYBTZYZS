using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetPatientConsultationsQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetPatientConsultationsQuery, Result<PagedResult<ConsultationDetailDto>>>
{
    public async Task<Result<PagedResult<ConsultationDetailDto>>> Handle(
        GetPatientConsultationsQuery request, CancellationToken cancellationToken)
    {
        var medicalCases = await repository.GetByPatientIdWithDetailsAsync(
            request.PatientId, cancellationToken);

        var consultations = medicalCases
            .Where(mc => mc.Consultation != null && !mc.Consultation.IsDeleted)
            .Select(mc =>
            {
                var dto = mapper.ToConsultationDetailDto(mc.Consultation!);
                dto.MedicalCaseId = mc.Id;
                dto.PatientId = mc.PatientId;
                dto.UserId = mc.UserId;
                dto.PatientName = mc.PatientName;
                dto.DoctorName = mc.DoctorName;
                dto.CreatedAt = mc.Consultation!.CreatedAt;
                dto.UpdatedAt = mc.Consultation.UpdatedAt;
                dto.CreatedBy = mc.Consultation.CreatedBy;
                return dto;
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var totalCount = consultations.Count;
        var paged = consultations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result<PagedResult<ConsultationDetailDto>>.Success(
            new PagedResult<ConsultationDetailDto>(paged, totalCount, request.Page, request.PageSize));
    }
}
