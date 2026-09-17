using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

/// <summary>
/// 标记是否需要开处方命令（三步流程 Step 2）。
/// </summary>
public sealed record SetPrescriptionFlagCommand(
    Guid Id,
    bool NeedsPrescription,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;
