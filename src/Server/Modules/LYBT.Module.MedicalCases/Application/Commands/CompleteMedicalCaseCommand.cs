using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.MedicalCases;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 完成医案命令。
/// skipWorkflowValidation=false: 验证 NeedsPrescription + 处方存在性 (BR-003)；
/// skipWorkflowValidation=true: 直接完成（Admin 强制关闭路径）。
/// </summary>
public sealed record CompleteMedicalCaseCommand(
    Guid MedicalCaseId,
    Guid OperatorId,
    bool IsAdmin = false,
    bool SkipWorkflowValidation = false
) : IRequest<Result<MedicalCase>>;
