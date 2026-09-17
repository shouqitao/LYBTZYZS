using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 记录打印完成命令处理器 — 委托 <see cref="IMedicalCaseCommandService.RecordPrintAsync"/>。
/// </summary>
public sealed class RecordPrintCommandHandler
    : IRequestHandler<RecordPrintCommand, Result<bool>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public RecordPrintCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<bool>> Handle(
        RecordPrintCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.RecordPrintAsync(
            request.Id, request.PrintType, request.PrinterName,
            request.OperatorId, request.OperatorName, cancellationToken);
    }
}
