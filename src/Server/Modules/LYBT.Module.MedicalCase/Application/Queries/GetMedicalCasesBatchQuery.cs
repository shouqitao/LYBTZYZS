using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCasesBatchQuery(
    List<Guid> Ids
) : IRequest<Result<List<MedicalCaseDetailDto>>>;
