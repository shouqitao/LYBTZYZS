using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 更新医案状态命令（P1-10：Completed 分支由 StateService 统一分派）。
/// </summary>
public sealed record UpdateMedicalCaseStatusCommand(
    Guid Id,
    MedicalCaseStatus Status,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;
