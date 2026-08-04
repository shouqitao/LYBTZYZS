using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record AddPrintLogCommand(
    Guid MedicalCaseId,
    int PrintType,
    bool IsSuccess,
    string? PrinterName,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<bool>>;
