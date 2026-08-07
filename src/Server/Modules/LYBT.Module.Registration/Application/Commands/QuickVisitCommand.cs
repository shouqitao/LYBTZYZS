using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

public record QuickVisitCommand(
    QuickVisitInputDto Input,
    Guid DoctorId,
    string DoctorName
) : IRequest<Result<QuickVisitResultDto>>;


