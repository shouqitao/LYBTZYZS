using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetPatientPrescriptionsQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetPatientPrescriptionsQuery, Result<PagedResult<PrescriptionDetailDto>>>
{
    public async Task<Result<PagedResult<PrescriptionDetailDto>>> Handle(
        GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var medicalCases = await repository.GetByPatientIdWithDetailsAsync(
            request.PatientId, cancellationToken);

        var prescriptions = medicalCases
            .Where(mc => mc.Prescription != null && !mc.Prescription.IsDeleted)
            .Select(mc =>
            {
                var p = mc.Prescription!;
                var dto = mapper.ToPrescriptionDetailDto(p);
                dto.MedicalCaseId = mc.Id;
                dto.CreatedAt = p.CreatedAt;
                dto.UpdatedAt = p.UpdatedAt;
                dto.Items = p.Items?.Select(mapper.ToPrescriptionItemDto).ToList()
                    ?? new List<PrescriptionItemDto>();
                dto.SingleDosePrice = p.Items?.Sum(x => x.Amount) ?? 0;
                dto.TotalPrice = dto.SingleDosePrice * p.DosageCount * p.Discount;
                dto.TotalWeight = p.Items?.Sum(x => x.Dosage) ?? 0;
                return dto;
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var totalCount = prescriptions.Count;
        var paged = prescriptions
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result<PagedResult<PrescriptionDetailDto>>.Success(
            new PagedResult<PrescriptionDetailDto>(paged, totalCount, request.Page, request.PageSize));
    }
}
