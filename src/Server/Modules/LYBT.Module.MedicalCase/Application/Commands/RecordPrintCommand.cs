using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record RecordPrintCommand(
    Guid MedicalCaseId,
    int PrintType,
    string? PrinterName,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<bool>>;
