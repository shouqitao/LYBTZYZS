using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 批量检查患者引用关系查询。
/// </summary>
public record BatchCheckPatientReferenceQuery(
    List<Guid> PatientIds
) : IRequest<Result<List<PatientReferenceCheckDto>>>;
