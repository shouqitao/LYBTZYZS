using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

public record ResetPasswordResult(string TemporaryPassword);

public record ResetPasswordCommand(
    Guid Id
) : IRequest<Result<ResetPasswordResult>>;
