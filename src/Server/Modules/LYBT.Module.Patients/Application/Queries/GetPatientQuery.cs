using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 根据ID获取患者详情查询。
/// </summary>
public record GetPatientQuery(Guid Id) : IRequest<Result<PatientDetailDto>>;


