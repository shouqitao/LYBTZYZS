using LYBT.Entities.Users;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户批量操作服务实现
    /// 从 UserService 中分离 BatchDelete / BatchUpdateStatus 职责，遵循单一职责原则
    /// </summary>
    public class UserBatchOperationService : BaseService<User>, IUserBatchOperationService
    {
        private readonly IUserRepository _repository;
        private readonly ICrossModuleAuthService _authService;

        public UserBatchOperationService(
            IUserRepository repository,
            ILogger<UserBatchOperationService> logger,
            ICrossModuleAuthService authService)
            : base(logger)
        {
            _repository = repository;
            _authService = authService;
        }

        #region 权限检查辅助方法

        /// <summary>
        /// 检查当前用户是否可以管理目标用户
        /// 权限规则：
        /// - SuperAdmin（100）可以管理所有用户（Admin、Doctor、Receptionist）
        /// - Admin（10）可以管理 Doctor 和 Receptionist，但不能管理 Admin 或 SuperAdmin
        /// - Doctor（1）不能管理其他用户
        /// - Receptionist（0）不能管理其他用户
        /// </summary>
        private static bool CanManageUser(UserRole? currentUserRole, UserRole? targetUserRole)
        {
            if (!currentUserRole.HasValue || !targetUserRole.HasValue)
                return false;

            return currentUserRole.Value switch
            {
                UserRole.SuperAdmin => true,
                UserRole.Admin => targetUserRole.Value is UserRole.Doctor or UserRole.Receptionist,
                UserRole.Doctor => false,
                _ => false
            };
        }

        /// <summary>
        /// 检查是否可以删除指定用户（包含最后一个保护）
        /// </summary>
        private async Task<Result> CanDeleteUserAsync(Guid userId, UserRole currentRole, UserRole targetRole, CancellationToken cancellationToken = default)
        {
            if (!CanManageUser(currentRole, targetRole))
            {
                return Result.Failure(GenericErrorCode.Forbidden, "您没有权限删除该用户");
            }

            if (targetRole == UserRole.SuperAdmin || targetRole == UserRole.Admin)
            {
                var users = await _repository.FindAsync(u => u.Role == targetRole, cancellationToken);
                var count = users.Count();
                if (count <= 1)
                {
                    var roleName = targetRole == UserRole.SuperAdmin ? "超级管理员" : "管理员";
                    return Result.Failure(GenericErrorCode.CannotDeleteSysAdmin, $"不能删除最后一个{roleName}");
                }
            }

            return Result.Success();
        }

        /// <summary>
        /// USER-D05 / CODE-04: sysadmin 账户硬兜底检查。
        /// </summary>
        private static bool IsSysAdmin(User user) => user.UserName == "sysadmin";

        #endregion

        /// <inheritdoc />
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid? currentUserId, UserRole currentRole, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // ERR-012: 修复ex.Message暴露

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count
            };

            if (ids.Count == 0)
            {
                return Result<BatchOperationResultDto>.Failure(GenericErrorCode.ValidationFailed, "请至少选择一个用户");
            }

            foreach (var id in ids)
            {
                // 不能删除自己
                if (currentUserId.HasValue && id == currentUserId.Value)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "不能删除自己"
                    });
                    result.FailureCount++;
                    continue;
                }

                var user = await _repository.GetByIdAsync(id, cancellationToken);
                if (user == null)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "用户不存在"
                    });
                    result.FailureCount++;
                    continue;
                }

                // USER-D05 / CODE-04: sysadmin 硬兜底
                if (IsSysAdmin(user))
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = "系统管理员账号不可被删除"
                    });
                    result.FailureCount++;
                    continue;
                }

                // 权限检查
                var permissionCheck = await CanDeleteUserAsync(id, currentRole, user.Role, cancellationToken);
                if (!permissionCheck.IsSuccess)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = permissionCheck.Message ?? "无权限删除"
                    });
                    result.FailureCount++;
                    continue;
                }

                // CODE-34: 标记软删除，不立即 SaveChanges，最后统一提交
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;
                result.SuccessCount++;
                _logger.LogInformation("[SVC] UserBatch.BatchDelete -> ItemMarked - UserId={UserId} UserName={UserName}", id, user.UserName);
            }

            // CODE-34: 统一 SaveChanges 确保单事务
            if (result.SuccessCount > 0)
            {
                await _repository.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("[SVC] UserBatch.BatchDelete completed - TotalCount={Total} SuccessCount={Success} FailureCount={Failure}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return Result<BatchOperationResultDto>.Success(result);
        }

        /// <inheritdoc />
        public async Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status, Guid? currentUserId, UserRole currentRole, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // ERR-012: 修复ex.Message暴露

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count
            };
            var statusText = status == CommonStatus.Enabled ? "启用" : "禁用";

            foreach (var id in ids)
            {
                // 不能修改自己的状态
                if (currentUserId.HasValue && id == currentUserId.Value)
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = $"不能{statusText}当前登录用户"
                    });
                    continue;
                }

                var user = await _repository.GetByIdAsync(id, cancellationToken);
                if (user == null || user.IsDeleted)
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "用户不存在"
                    });
                    continue;
                }

                // USER-D05 / CODE-04: sysadmin 硬兜底
                if (IsSysAdmin(user))
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = "系统管理员账号不可被管理"
                    });
                    continue;
                }

                // S2-08: 权限检查 - 逐个校验 CanManageUser
                if (!CanManageUser(currentRole, user.Role))
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = $"无权限{statusText}该用户"
                    });
                    continue;
                }

                // S2-08: 最后管理员保护 (禁用场景)
                if (status == CommonStatus.Disabled
                    && user.Status == CommonStatus.Enabled
                    && user.Role >= UserRole.Admin)
                {
                    var activeAdmins = await _repository.FindAsync(
                        u => u.Role >= UserRole.Admin && u.Status == CommonStatus.Enabled, cancellationToken);
                    if (activeAdmins.Count() <= 1)
                    {
                        result.FailureCount++;
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Name = user.UserName,
                            Reason = "不能禁用最后一个管理员"
                        });
                        continue;
                    }
                }

                user.Status = status;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user, cancellationToken);
                result.SuccessCount++;

                // X3: 禁用用户时撤销所有 Token
                if (status == CommonStatus.Disabled)
                {
                    await _authService.RevokeUserTokensAsync(id, "批量禁用，强制登出");
                }

                _logger.LogInformation("[SVC] UserBatch.BatchUpdateStatus → ItemSuccess - UserId={UserId} Status={Status}", id, status);
            }

            result.Message = $"批量{statusText}完成: 成功 {result.SuccessCount} 个, 失败 {result.FailureCount} 个";

            _logger.LogInformation("[SVC] UserBatch.BatchUpdateStatus completed - Total={Total} Success={Success} Failure={Failure}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return Result<BatchOperationResultDto>.Success(result);
        }
    }
}