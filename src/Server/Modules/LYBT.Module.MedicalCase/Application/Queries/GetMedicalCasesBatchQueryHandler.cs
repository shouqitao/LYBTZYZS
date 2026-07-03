using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class GetMedicalCasesBatchQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<GetMedicalCasesBatchQuery, Result<List<MedicalCaseDetailDto>>>
{
    public async Task<Result<List<MedicalCaseDetailDto>>> Handle(
        GetMedicalCasesBatchQuery request, CancellationToken cancellationToken)
    {
        var medicalCases = await repository.GetBatchWithDetailsAsync(request.Ids, cancellationToken);
        var dtos = mapper.ToDetailDtos(medicalCases);
        return Result<List<MedicalCaseDetailDto>>.Success(dtos);
    }
}
