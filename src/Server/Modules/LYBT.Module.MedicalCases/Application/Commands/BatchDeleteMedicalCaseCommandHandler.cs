using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 批量删除医案命令处理器 — 委托 <see cref="IMedicalCaseCommandService.BatchDeleteAsync"/>。
/// </summary>
public sealed class BatchDeleteMedicalCaseCommandHandler
    : IRequestHandler<BatchDeleteMedicalCaseCommand, Result<BatchOperationResultDto>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public BatchDeleteMedicalCaseCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.BatchDeleteAsync(
            request.Ids, request.OperatorId, request.IsAdmin, cancellationToken);
    }
}
