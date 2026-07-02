using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class SuspendMedicalCaseCommandHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<SuspendMedicalCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        SuspendMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.Suspend();

        await repository.UpdateAsync(medicalCase, cancellationToken);
        return Result<bool>.Success(true);
    }
}


