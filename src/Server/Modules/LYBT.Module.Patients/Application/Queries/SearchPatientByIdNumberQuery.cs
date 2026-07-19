using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Patients.Application.Queries;

public record SearchPatientByIdNumberQuery(string IdNumber) : IRequest<Result<PatientDetailDto>>;
