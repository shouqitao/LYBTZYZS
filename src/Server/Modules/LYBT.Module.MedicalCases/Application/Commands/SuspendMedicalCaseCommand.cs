using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 挂起医案命令（保存当前数据，状态置为 Suspended，不触发完成验证）。
/// </summary>
public sealed record SuspendMedicalCaseCommand(
    Guid Id,
    ConsultationInputDto? Request,
    Guid OperatorId,
    bool IsAdmin = false
) : IRequest<Result<MedicalCaseDetailDto>>;
