using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCaseQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetMedicalCaseQuery, Result<MedicalCaseDetailDto>>
{
    public async Task<Result<MedicalCaseDetailDto>> Handle(
        GetMedicalCaseQuery request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, "医案不存在");

        var dto = mapper.MapToMedicalCaseDetailDto(medicalCase);
        return Result<MedicalCaseDetailDto>.Success(dto);
    }
}


