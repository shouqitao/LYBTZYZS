using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 恢复已软删除的患者。
/// </summary>
public record RestorePatientCommand(
    Guid PatientId,
    Guid OperatorId
) : IRequest<Result<PatientDetailDto>>;
