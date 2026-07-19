using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class RecordPrintCommandHandler(
    IMedicalCaseRepository repository
) : IRequestHandler<RecordPrintCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RecordPrintCommand request, CancellationToken cancellationToken)
    {
        var medicalCase = await repository.GetByIdAsync(request.MedicalCaseId, cancellationToken);
        if (medicalCase == null)
            return Result<bool>.Failure(ErrorCode.NotFound, "医案不存在");

        var now = DateTime.UtcNow;

        medicalCase.IsPrinted = true;
        medicalCase.PrintCount += 1;
        medicalCase.LastPrintedAt = now;
        medicalCase.PrintVersion += 1;

        await repository.UpdateAsync(medicalCase, cancellationToken);

        var printLog = new MedicalCasePrintLog
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = request.MedicalCaseId,
            PrintType = request.PrintType,
            PrintVersion = medicalCase.PrintVersion,
            PrinterName = request.PrinterName,
            PrintedBy = request.OperatorName,
            PrintedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddPrintLogAsync(printLog, cancellationToken);

        return Result<bool>.Success(true);
    }
}
