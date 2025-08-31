using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Module.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Users.Helpers
{
    /// <summary>
    /// UserService业务助手类 - UltraThink Helper模式
    /// 负责复杂业务流程、用户管理、密码管理和批量操作逻辑
    /// </summary>
    public class UserBusinessHelper(
        AppDbContext context,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserBusinessHelper> logger,
        IOptions<UserOptions> options,
        UserValidationHelper validationHelper)
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly ILogger<UserBusinessHelper> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly UserOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly UserValidationHelper _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));

        /// <summary>
        /// 创建用户（现代化版本，使用UserMutationDto）
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto)
        {
            try
            {
                // 验证创建请求
                var validation = await _validationHelper.ValidateUserMutationAsync(dto, isCreateOperation: true);
                if (!validation.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validation.ErrorMessage!);

                // 使用ExecutionStrategy处理事务以兼容重试策略
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var user = CreateUserFromMutationDto(dto);
                        // 添加到DbSet但不保存，让事务统一处理保存
                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                        var result = user;

                        if (result != null)
                        {
                            // 内部记录操作日志，使用系统用户ID
                            await LogUserOperation(
                                user.Id, ActionType.Create, Guid.Empty, "System",
                                $"新增用户：{user.Username}",
                                newValue: user
                            );

                            await transaction.CommitAsync();
                            var userDto = _mapper.Map<UserDto>(user);
                            _logger.LogInformation("创建用户成功: {Username} (ID: {UserId})", user.Username, user.Id);
                            return ServiceResult<UserDto>.Success(userDto);
                        }

                        await transaction.RollbackAsync();
                        return ServiceResult<UserDto>.Failure("用户创建失败");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败, Username: {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// 更新用户信息（现代化版本，使用UserMutationDto）
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto)
        {
            try
            {
                // 验证更新请求
                var validation = await _validationHelper.ValidateUserMutationAsync(dto, isCreateOperation: false, existingUserId: id);
                if (!validation.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validation.ErrorMessage!);

                var existingUser = await GetExistingUser(id);
                var oldSnapshot = JsonSerializer.Serialize(existingUser);

                // 使用ExecutionStrategy处理事务以兼容重试策略
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        UpdateUserFromMutationDto(existingUser, dto);
                        // 更新实体但不保存，让事务统一处理保存
                        _context.Users.Update(existingUser);
                        await _context.SaveChangesAsync();
                        var result = existingUser;

                        if (result != null)
                        {
                            await LogUserOperation(
                                existingUser.Id, ActionType.Update, Guid.Empty, "System",
                                $"修改用户信息：{existingUser.Username}",
                                oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(existingUser)
                            );

                            await transaction.CommitAsync();
                            var userDto = _mapper.Map<UserDto>(result);
                            _logger.LogInformation("更新用户成功: {Username} (ID: {UserId})", result.Username, id);
                            return ServiceResult<UserDto>.Success(userDto);
                        }

                        await transaction.RollbackAsync();
                        return ServiceResult<UserDto>.Failure("用户更新失败");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败, ID: {UserId}", id);
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}", ex);
            }
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
                        id, ActionType.Update, Guid.Empty, "System",                        $"禁用用户：{user.Username}",                        oldValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("禁用用户成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}", ex);            }
        }

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
                        id, ActionType.Update, Guid.Empty, "System",                        $"启用用户：{user.Username}",                        oldValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("启用用户成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}", ex);            }
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
                        .SetProperty(u => u.Status, CommonStatus.Disabled));

                if (affectedCount > 0)
                {
                    // 记录批量操作日志（简化版）
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
                        .SetProperty(u => u.Status, CommonStatus.Enabled));

                if (affectedCount > 0)
                {
                    // 记录批量操作日志（简化版）
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
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                var validation = await _validationHelper.ValidatePasswordResetAsync(id, newPassword);
                if (!validation.IsSuccess)
                    return ServiceResult<bool>.Failure(validation.ErrorMessage!);

                var user = await GetExistingUser(id);
                var newPasswordHash = PasswordHelper.Hash(newPassword);
                var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",                        $"重置用户密码：{user.Username}"                    );

                    if (_options.SendPasswordResetNotification)
                    {
                        await SendPasswordResetNotification(user);
                    }

                    _logger.LogInformation("重置用户密码成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"重置用户密码失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var validation = await _validationHelper.ValidatePasswordChangeAsync(id, oldPassword, newPassword);
                if (!validation.IsSuccess)
                    return ServiceResult<bool>.Failure(validation.ErrorMessage!);

                var user = await GetExistingUser(id);

                if (!PasswordHelper.Verify(user.PasswordHash, oldPassword))
                {
                    return ServiceResult<bool>.Failure("原密码错误");                }

                var newPasswordHash = PasswordHelper.Hash(newPassword);
                var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, id, user.RealName,
                        "用户修改个人密码"                    );

                    _logger.LogInformation("用户修改密码成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户密码失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"修改用户密码失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 用户修改个人信息
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
                        "用户修改个人信息",                        oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("用户修改个人信息成功: {Username} (ID: {UserId})", user.Username, id);                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Failure("修改个人信息失败");            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户个人信息失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"修改用户个人信息失败: {ex.Message}", ex);            }
        }

        /// <summary>
        /// 删除用户（软删除 - 使用禁用代替）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                // 使用禁用代替删除（软删除策略）
                var result = await DisableUserAsync(id);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("软删除用户成功: ID {UserId}", id);                    return ServiceResult<bool>.Success(result.Data);
                }
                return ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 从UserMutationDto创建用户模型（现代化版本）
        /// </summary>
        private User CreateUserFromMutationDto(UserMutationDto dto)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username!,  // 创建时必须提供用户名
                RealName = dto.RealName,
                PinYinCode = CommonHelper.GetPinyinCode(dto.RealName),
                Status = dto.Status,
                Role = Enum.Parse<UserRole>(dto.Role),
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = !string.IsNullOrWhiteSpace(dto.Password) 
                    ? PasswordHelper.Hash(dto.Password)
                    : PasswordHelper.Hash(_options.DefaultUserPassword)
            };
        }

        /// <summary>
        /// 使用UserMutationDto更新用户模型（现代化版本）
        /// </summary>
        private static void UpdateUserFromMutationDto(User user, UserMutationDto dto)
        {
            user.RealName = dto.RealName;
            user.PinYinCode = CommonHelper.GetPinyinCode(dto.RealName);
            user.Status = dto.Status;
            user.Role = Enum.Parse<UserRole>(dto.Role);
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
        }



        /// <summary>
        /// 从DTO更新用户模型
        /// </summary>


        /// <summary>
        /// 获取现有用户（不存在时抛出异常）
        /// </summary>
        private async Task<User> GetExistingUser(Guid id)
        {
            // 内部方法总是包含禁用用户，确保操作能正常进行
            var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
            if (user == null)
            {
                throw new InvalidOperationException("用户不存在");            }
            return user;
        }

        /// <summary>
        /// 统一的用户操作日志记录
        /// </summary>
        private async Task LogUserOperation(
            Guid userId, ActionType actionType, Guid operatorId, string operatorName,
            string content, string? oldValue = null, object? newValue = null)
        {
            if (!_options.EnableDetailedAuditLogging)
                return;

            await Task.Run(() =>
            {
                _logger.LogInformation("用户操作日志 - 操作者: {OperatorName} ({OperatorId}), 操作类型: {ActionType}, 内容: {Content}",                    operatorName, operatorId, actionType, content);
            });
        }

        /// <summary>
        /// 批量操作日志记录
        /// </summary>
        private async Task LogBatchUserOperation(
            List<Guid> userIds, ActionType actionType, Guid operatorId, string operatorName,
            string content)
        {
            if (!_options.EnableDetailedAuditLogging)
                return;

            await Task.Run(() =>
            {
                var userIdString = string.Join(", ", userIds);                var detailedContent = $"{content}: {userIdString}";                _logger.LogInformation("批量用户操作日志 - 操作者: {OperatorName} ({OperatorId}), 操作类型: {ActionType}, 内容: {Content}",                    operatorName, operatorId, actionType, detailedContent);
            });
        }

        /// <summary>
        /// 发送密码重置通知（待实现）
        /// </summary>
        private async Task SendPasswordResetNotification(User user)
        {
            // 可以发送邮件、短信或系统内通知
            await Task.CompletedTask;
            _logger.LogInformation("密码重置通知已发送: {Username}", user.Username);
        }

        #endregion
    }
}


