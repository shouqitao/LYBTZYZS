using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 标记是否需要开处方命令处理器 — 委托 <see cref="IMedicalCaseCommandService.SetPrescriptionFlagWithDetailAsync"/>。
/// </summary>
public sealed class SetPrescriptionFlagCommandHandler
    : IRequestHandler<SetPrescriptionFlagCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public SetPrescriptionFlagCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        SetPrescriptionFlagCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.SetPrescriptionFlagWithDetailAsync(
            request.Id, request.NeedsPrescription, request.OperatorId, request.IsAdmin, cancellationToken);
    }
}
