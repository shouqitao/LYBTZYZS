using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 取消医案命令（US-MC-014：物理删除聚合 + 审计记录；非当天本人取消时 Reason 必填）。
/// </summary>
public sealed record CancelMedicalCaseCommand(
    Guid Id,
    Guid OperatorId,
    bool IsAdmin = false,
    string? Reason = null
) : IRequest<Result<MedicalCaseDetailDto>>;
