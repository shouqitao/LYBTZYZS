using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 记录打印完成命令（US-PRINT-004：回写打印状态 + 记录日志）。
/// </summary>
public sealed record RecordPrintCommand(
    Guid Id,
    int PrintType,
    string? PrinterName,
    Guid OperatorId,
    string OperatorName
) : IRequest<Result<bool>>;
