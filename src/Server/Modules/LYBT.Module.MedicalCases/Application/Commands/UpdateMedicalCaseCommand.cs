using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 更新医案命令（Id 有值；含 Consultation/Prescription 聚合保存）。
/// </summary>
public sealed record UpdateMedicalCaseCommand(
    MedicalCaseInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin = false
) : IRequest<Result<MedicalCaseDetailDto>>;
