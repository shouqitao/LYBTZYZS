using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class BatchDeleteUsersCommandHandler : IRequestHandler<BatchDeleteUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;

    public BatchDeleteUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteUsersCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto { TotalCount = request.Ids.Count };

        foreach (var id in request.Ids)
        {
            if (id == request.CurrentUserId)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "不能删除自己" });
                result.FailureCount++;
                continue;
            }

            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "用户不存在" });
                result.FailureCount++;
                continue;
            }

            if (user.IsSysAdmin)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "系统管理员账号不可被删除" });
                result.FailureCount++;
                continue;
            }

            if (!request.IsAdmin)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "无权限删除" });
                result.FailureCount++;
                continue;
            }

            try
            {
                user.SoftDelete(request.CurrentUserId);
                await _userRepository.UpdateAsync(user, cancellationToken);
                result.SuccessCount++;
            }
            catch (InvalidOperationException ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = ex.Message });
                result.FailureCount++;
            }
        }

        result.Message = $"批量删除完成: 成功{result.SuccessCount}个, 失败{result.FailureCount}个";
        return Result<BatchOperationResultDto>.Success(result);
    }
}


