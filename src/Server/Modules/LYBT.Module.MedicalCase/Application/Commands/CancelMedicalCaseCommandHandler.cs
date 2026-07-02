using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class CancelMedicalCaseCommandHandler(
    IMedicalCaseRepository repository,
    IRegistrationCrossModuleService registrationCrossModule
) : IRequestHandler<CancelMedicalCaseCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CancelMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        medicalCase.SoftDelete();

        await repository.UpdateAsync(medicalCase, cancellationToken);
        await registrationCrossModule.HandleMedicalCaseCancelledAsync(request.Id, cancellationToken);

        return Result<bool>.Success(true);
    }
}


