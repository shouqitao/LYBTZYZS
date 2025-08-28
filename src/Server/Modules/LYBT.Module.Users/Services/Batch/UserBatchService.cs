using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services.Batch
{
    /// <summary>
    /// 用户批量操作服务实现
    /// UltraThink重构：专注于用户的批量操作功能
    /// 代码行数：约150行，符合500行以下标准
    /// </summary>
    public class UserBatchService : IUserBatchService
    {
        private readonly AppDbContext _context;
        private readonly UserValidationHelper _validationHelper;
        private readonly ILogger<UserBatchService> _logger;

        public UserBatchService(
            AppDbContext context,
            UserValidationHelper validationHelper,
            ILogger<UserBatchService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 批量启用用户（优化版 - 使用ExecuteUpdate）
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableUsersAsync(List<Guid> ids)
        {
            try
            {
                var validation = _validationHelper.ValidateBatchOperation(ids);
                if (!validation.IsSuccess)
                    return ServiceResult<int>.Failure(validation.ErrorMessage!);

                // 使用EF Core的ExecuteUpdate进行批量更新，避免加载到内存
                var affectedCount = await _context.Users
                    .Where(u => ids.Contains(u.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, CommonStatus.Enabled)
                        .SetProperty(u => u.UpdateTime, DateTime.Now));

                if (affectedCount > 0)
                {
                    // 记录批量操作日志
                    _logger.LogInformation("批量启用用户成功: 影响{Count}条记录", affectedCount);                    await LogBatchUserOperation(
                        ids, ActionType.Update, Guid.Empty, "System",                        $"批量启用 {affectedCount} 个用户"                    );
                }

                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败, IDs: {UserIds}", string.Join(",", ids));                return ServiceResult<int>.Failure($"批量启用用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 批量禁用用户（优化版 - 使用ExecuteUpdate）
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableUsersAsync(List<Guid> ids)
        {
            try
            {
                var validation = _validationHelper.ValidateBatchOperation(ids);
                if (!validation.IsSuccess)
                    return ServiceResult<int>.Failure(validation.ErrorMessage!);

                // 使用EF Core的ExecuteUpdate进行批量更新，避免加载到内存
                var affectedCount = await _context.Users
                    .Where(u => ids.Contains(u.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, CommonStatus.Disabled)
                        .SetProperty(u => u.UpdateTime, DateTime.Now));

                if (affectedCount > 0)
                {
                    // 记录批量操作日志
                    _logger.LogInformation("批量禁用用户成功: 影响{Count}条记录", affectedCount);                    await LogBatchUserOperation(
                        ids, ActionType.Update, Guid.Empty, "System",                        $"批量禁用 {affectedCount} 个用户"                    );
                }

                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败, IDs: {UserIds}", string.Join(",", ids));                return ServiceResult<int>.Failure($"批量禁用用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 批量删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult<int>> BatchDeleteUsersAsync(List<Guid> ids)
        {
            try
            {
                var validation = _validationHelper.ValidateBatchOperation(ids);
                if (!validation.IsSuccess)
                    return ServiceResult<int>.Failure(validation.ErrorMessage!);

                // 使用EF Core的ExecuteUpdate进行批量软删除
                var affectedCount = await _context.Users
                    .Where(u => ids.Contains(u.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, CommonStatus.Disabled)
                        .SetProperty(u => u.UpdateTime, DateTime.Now));

                if (affectedCount > 0)
                {
                    // 记录批量操作日志
                    _logger.LogInformation("批量删除用户成功: 影响{Count}条记录", affectedCount);                    await LogBatchUserOperation(
                        ids, ActionType.Delete, Guid.Empty, "System",                        $"批量删除 {affectedCount} 个用户"                    );
                }

                return ServiceResult<int>.Success(affectedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除用户失败, IDs: {UserIds}", string.Join(",", ids));                return ServiceResult<int>.Failure($"批量删除用户失败: {ex.Message}", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 记录批量用户操作日志
        /// </summary>
        private async Task LogBatchUserOperation(
            List<Guid> targetUserIds, ActionType actionType, Guid operatorId, string operatorName,
            string description)
        {
            try
            {
                // TODO: 实现批量操作日志记录
                _logger.LogInformation(
                    "批量用户操作日志 - 目标用户数: {Count}, 操作类型: {ActionType}, 操作者: {OperatorName}, 描述: {Description}",                    targetUserIds.Count, actionType, operatorName, description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录批量用户操作日志失败");
            }
        }

        #endregion
    }
}
