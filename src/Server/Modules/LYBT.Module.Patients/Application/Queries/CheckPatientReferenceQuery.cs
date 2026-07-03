using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 检查患者是否被医案引用。
/// </summary>
public record CheckPatientReferenceQuery(
    Guid PatientId
) : IRequest<Result<PatientReferenceCheckDto>>;
