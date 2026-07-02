using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record BatchDeleteMedicalCasesCommand(
    List<Guid> Ids,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<BatchOperationResultDto>>;


