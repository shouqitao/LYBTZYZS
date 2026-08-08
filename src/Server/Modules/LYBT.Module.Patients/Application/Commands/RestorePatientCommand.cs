using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 恢复已删除患者命令。
/// </summary>
public record RestorePatientCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result<PatientDetailDto>>;
