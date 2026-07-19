using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class CompleteMedicalCaseCommandHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<CompleteMedicalCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CompleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.Complete();

        await repository.UpdateAsync(medicalCase, cancellationToken);
        return Result<bool>.Success(true);
    }
}


