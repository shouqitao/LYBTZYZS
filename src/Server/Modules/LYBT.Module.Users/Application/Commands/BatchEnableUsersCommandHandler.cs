using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Users.Application.Commands;

public class BatchEnableUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
      IRequestHandler<BatchEnableUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;

    public BatchEnableUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchEnableUsersCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, Guid.Empty, cancellationToken);

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)
        => _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.Status = CommonStatus.Enabled;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.UserNotFound);
    protected override string OperationName => "启用";

    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override Task<string?> ValidateAsync(
        ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)
        => user.IsSysAdmin
            ? Task.FromResult<string?>("系统管理员账号不可被操作")
            : Task.FromResult<string?>(null);
}
