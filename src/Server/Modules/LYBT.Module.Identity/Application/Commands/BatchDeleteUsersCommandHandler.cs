using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Entities.Users;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Identity.Application.Commands;

public class BatchDeleteUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
      IRequestHandler<BatchDeleteUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;
    private bool _isAdmin;

    public BatchDeleteUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteUsersCommand request, CancellationToken cancellationToken)
    {
        _isAdmin = request.IsAdmin;
        return await ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);
    }

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct)
        => _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(ApplicationUser user, Guid operatorId, CancellationToken ct)
    {
        user.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.UserNotFound);
    protected override string OperationName => "删除";
    protected override Type CaughtExceptionType => typeof(InvalidOperationException);

    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override Task<string?> ValidateAsync(
        ApplicationUser user, Guid id, Guid operatorId, CancellationToken ct)
    {
        if (id == operatorId)
            return Task.FromResult<string?>("不能删除自己");
        if (user.IsSysAdmin)
            return Task.FromResult<string?>("系统管理员账号不可被删除");
        if (!_isAdmin)
            return Task.FromResult<string?>("无权限删除");
        return Task.FromResult<string?>(null);
    }
}
