using LYBT.Entities.Users;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Identity.Application.Commands;

public class BatchDeleteUsersCommandHandler
    : BatchOperationHandlerBase<ApplicationUser>,
        IRequestHandler<BatchDeleteUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;
    private UserRole _operatorRole;

    public BatchDeleteUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteUsersCommand request,
        CancellationToken cancellationToken
    )
    {
        // P3 (US-USER-012): 单次批量上限 100 条（T1.3 抽 BatchOptions.DefaultMaxBatchSize）
        if (request.Ids.Count > BatchOptions.DefaultMaxBatchSize)
        {
            return Result<BatchOperationResultDto>.Failure(
                ErrorCode.InvalidRequest,
                $"单次批量操作数量不能超过 {BatchOptions.DefaultMaxBatchSize} 条，当前 {request.Ids.Count} 条"
            );
        }

        _operatorRole = request.OperatorRole;
        return await ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);
    }

    protected override Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _userRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(ApplicationUser user, CancellationToken ct) =>
        _userRepository.UpdateAsync(user, ct);

    protected override Task ApplyOperationAsync(
        ApplicationUser user,
        Guid operatorId,
        CancellationToken ct
    )
    {
        user.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.UserNotFound);
    protected override string OperationName => "删除";
    protected override Type CaughtExceptionType => typeof(InvalidOperationException);

    protected override string? GetEntityName(ApplicationUser user) => user.UserName;

    protected override Task<string?> ValidateAsync(
        ApplicationUser user,
        Guid id,
        Guid operatorId,
        CancellationToken ct
    )
    {
        // UPDATEUSER-HIERARCHY-FIX: 层级校验（原 IsAdmin bool 粗粒度——Admin 批量删 Admin 违反 USER-D05）
        var guardResult = UserHierarchyGuard.Validate<object>(
            operatorId,
            id,
            _operatorRole,
            user.IsSysAdmin,
            user.Role
        );
        if (guardResult != null)
            return Task.FromResult<string?>(guardResult.Error ?? "无权限删除");
        return Task.FromResult<string?>(null);
    }
}
