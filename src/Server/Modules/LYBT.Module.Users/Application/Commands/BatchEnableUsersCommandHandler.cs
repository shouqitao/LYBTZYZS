using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class BatchEnableUsersCommandHandler : IRequestHandler<BatchEnableUsersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUserRepository _userRepository;

    public BatchEnableUsersCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchEnableUsersCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto { TotalCount = request.Ids.Count };

        foreach (var id in request.Ids)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "用户不存在" });
                result.FailureCount++;
                continue;
            }

            if (user.IsSysAdmin)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = "系统管理员账号不可被操作" });
                result.FailureCount++;
                continue;
            }

            try
            {
                user.Status = CommonStatus.Enabled;
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = user.UserName, Reason = ex.Message });
                result.FailureCount++;
            }
        }

        result.Message = $"批量启用完成: 成功{result.SuccessCount}个, 失败{result.FailureCount}个";
        return Result<BatchOperationResultDto>.Success(result);
    }
}
