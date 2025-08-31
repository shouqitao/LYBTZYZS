using System.Text.Json;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Helpers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Module.Users.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services.Account
{
    /// <summary>
    /// 用户账户状态管理服务实现
    /// UltraThink重构：专注于用户账户状态和个人资料管理
    /// 代码行数：约120行，符合500行以下标准
    /// </summary>
    public class UserAccountService(
        IUserRepository userRepository,
        UserValidationHelper validationHelper,
        ILogger<UserAccountService> logger) : IUserAccountService
    {
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly UserValidationHelper _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
        private readonly ILogger<UserAccountService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableUserAsync(Guid id)
        {
            try
            {
                var user = await GetExistingUser(id);
                var result = await _userRepository.EnableAsync(id);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",                        $"启用用户：{user.Username}",                        _oldValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("启用用户成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableUserAsync(Guid id)
        {
            try
            {
                var user = await GetExistingUser(id);
                var result = await _userRepository.DisableAsync(id);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",                        $"禁用用户：{user.Username}",                        _oldValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("禁用用户成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 用户修改个人资料
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, string realName, string phoneNumber)
        {
            try
            {
                var validation = await _validationHelper.ValidateProfileChangeAsync(id, realName, phoneNumber);
                if (!validation.IsSuccess)
                    return ServiceResult<bool>.Failure(validation.ErrorMessage!);

                var user = await GetExistingUser(id);
                var oldSnapshot = JsonSerializer.Serialize(user);

                user.RealName = realName;
                user.PinYinCode = CommonHelper.GetPinyinCode(realName);
                user.PhoneNumber = phoneNumber;

                var result = await _userRepository.UpdateAsync(user);

                if (result != null)
                {
                    await LogUserOperation(
                        id, ActionType.Update, id, user.RealName,
                        "用户修改个人资料",                        _oldValue: oldSnapshot, _newValue: JsonSerializer.Serialize(result)
                    );

                    _logger.LogInformation("用户修改个人资料成功: {Username} (ID: {UserId})", user.Username, id);                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户个人资料失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"修改用户个人资料失败: {ex.Message}", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 获取已存在的用户
        /// </summary>
        private async Task<User> GetExistingUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new InvalidOperationException($"用户不存在: {id}");            
            return user;
        }

        /// <summary>
        /// 记录用户操作日志
        /// </summary>
        private Task LogUserOperation(
            Guid targetUserId, ActionType actionType, Guid _operatorId, string operatorName, 
            string description, object? _oldValue = null, object? _newValue = null)
        {
            try
            {
                // TODO: 实现操作日志记录
                _logger.LogInformation(
                    "用户账户操作日志 - 目标用户: {TargetUserId}, 操作类型: {ActionType}, 操作者: {OperatorName}, 描述: {Description}",                    targetUserId, actionType, operatorName, description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录用户账户操作日志失败");
            }
            
            return Task.CompletedTask;
        }

        #endregion
    }
}
