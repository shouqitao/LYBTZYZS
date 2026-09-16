using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 更新医案命令处理器 — 委托 <see cref="IMedicalCaseCommandService.SaveWithDetailAsync"/>。
/// </summary>
public sealed class UpdateMedicalCaseCommandHandler
    : IRequestHandler<UpdateMedicalCaseCommand, Result<MedicalCaseDetailDto>>
{
    private readonly IMedicalCaseCommandService _commandService;

    public UpdateMedicalCaseCommandHandler(IMedicalCaseCommandService commandService)
    {
        _commandService = commandService;
    }

    public async Task<Result<MedicalCaseDetailDto>> Handle(
        UpdateMedicalCaseCommand request, CancellationToken cancellationToken)
    {
        return await _commandService.SaveWithDetailAsync(
            request.Input, request.CurrentUserId, request.IsAdmin, cancellationToken);
    }
}
