using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 创建医案命令（Id=null 创建；含 Consultation/Prescription 聚合保存）。
/// </summary>
public sealed record CreateMedicalCaseCommand(
    MedicalCaseInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin = false
) : IRequest<Result<MedicalCaseDetailDto>>;
