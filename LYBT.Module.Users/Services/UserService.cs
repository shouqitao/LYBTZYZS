using LYBT.Common.Enums.Logs;
using LYBT.Common.Enums.Users;
using LYBT.Common.Helpers;
using LYBT.Infrastructure.Logging;
using LYBT.Models.Users;
using LYBT.Module.Users.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LYBT.Module.Users.Services {

    /// <summary>
    /// 用户服务实现类（集成日志模块）
    /// </summary>
    public class UserService : IUserService {
        private readonly IUserRepository _userRepository;
        private readonly IUnifiedLogService _logService;
        private readonly UserOptions _options;

        public UserService(
            IUserRepository userRepository,
            IUnifiedLogService logService,
            IOptions<UserOptions> options) {
            _userRepository = userRepository;
            _logService = logService;
            _options = options.Value;
        }

        /// <summary>
        /// 分页/条件查找用户
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        public async Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query, UserRole currentUserRole) {
            // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
            bool includeDisabled = currentUserRole == UserRole.Admin;

            var (models, total) = await _userRepository.GetPagedAsync(query, includeDisabled);
            var users = models.Select(MapToUserDto).ToList();
            return (users, total);
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        public async Task<UserDto?> GetByIdAsync(Guid id, UserRole currentUserRole) {
            // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
            bool includeDisabled = currentUserRole == UserRole.Admin;

            var model = await _userRepository.GetByIdAsync(id, includeDisabled);
            return model != null ? MapToUserDto(model) : null;
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName) {
            await ValidateUserCreation(dto);

            var user = CreateUserFromDto(dto);
            var result = await _userRepository.AddAsync(user);

            if (result) {
                await LogUserOperation(
                    user.Id, ActionType.Create, operatorId, operatorName,
                    $"新增用户：{user.UserName}",
                    newValue: user
                );
            }

            return result;
        }

        /// <summary>
        /// 编辑用户
        /// </summary>
        public async Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName) {
            var existingUser = await GetExistingUser(dto.Id);
            var oldSnapshot = JsonSerializer.Serialize(existingUser);

            UpdateUserFromDto(existingUser, dto);
            var result = await _userRepository.UpdateAsync(existingUser);

            if (result) {
                await LogUserOperation(
                    existingUser.Id, ActionType.Edit, operatorId, operatorName,
                    $"修改用户信息：{existingUser.UserName}",
                    oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(existingUser)
                );
            }

            return result;
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await GetExistingUser(id);
            var result = await _userRepository.DisableAsync(id);

            if (result) {
                await LogUserOperation(
                    id, ActionType.Disable, operatorId, operatorName,
                    $"禁用用户：{user.UserName}",
                    oldValue: JsonSerializer.Serialize(user)
                );
            }

            return result;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await GetExistingUser(id);
            var result = await _userRepository.EnableAsync(id);

            if (result) {
                await LogUserOperation(
                    id, ActionType.Enable, operatorId, operatorName,
                    $"启用用户：{user.UserName}",
                    oldValue: JsonSerializer.Serialize(user)
                );
            }

            return result;
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            ValidateBatchOperation(ids);

            var users = await GetUsersByIds(ids);
            var updatedCount = await _userRepository.UpdateActiveStatusAsync(ids, false);

            if (updatedCount > 0) {
                await LogBatchUserOperation(
                    users, ActionType.Disable, operatorId, operatorName,
                    $"批量禁用 {updatedCount} 个用户"
                );
            }

            return updatedCount;
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            ValidateBatchOperation(ids);

            var users = await GetUsersByIds(ids);
            var updatedCount = await _userRepository.UpdateActiveStatusAsync(ids, true);

            if (updatedCount > 0) {
                await LogBatchUserOperation(
                    users, ActionType.Enable, operatorId, operatorName,
                    $"批量启用 {updatedCount} 个用户"
                );
            }

            return updatedCount;
        }

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await GetExistingUser(id);

            var newPasswordHash = PasswordHelper.Hash(_options.DefaultUserPassword);
            var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

            if (result) {
                await LogUserOperation(
                    id, ActionType.ResetPassword, operatorId, operatorName,
                    $"重置用户密码：{user.UserName}"
                );

                // TODO: 根据配置决定是否发送密码重置通知
                if (_options.SendPasswordResetNotification) {
                    await SendPasswordResetNotification(user);
                }
            }

            return result;
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var user = await GetExistingUser(id);

            if (!PasswordHelper.Verify(user.PasswordHash, oldPassword)) {
                throw new UnauthorizedAccessException("原密码错误");
            }

            var newPasswordHash = PasswordHelper.Hash(newPassword);
            var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

            if (result) {
                await LogUserOperation(
                    id, ActionType.Edit, id, user.RealName,
                    "用户修改个人密码"
                );
            }

            return result;
        }

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        public async Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber) {
            var user = await GetExistingUser(id);
            var oldSnapshot = JsonSerializer.Serialize(user);

            user.RealName = realName;
            user.PinyinCode = CommonHelper.GetPinyinCode(realName);
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            var result = await _userRepository.UpdateAsync(user);

            if (result) {
                await LogUserOperation(
                    id, ActionType.Edit, id, user.RealName,
                    "用户修改个人信息",
                    oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(user)
                );
            }

            return result;
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public List<UserRole> GetRoles() {
            return Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<List<UserDto>> GetActiveUsersAsync() {
            var users = await _userRepository.GetActiveUsersAsync();
            return users.Select(MapToUserDto).ToList();
        }

        #region 私有辅助方法

        /// <summary>
        /// 映射用户模型到DTO
        /// </summary>
        private UserDto MapToUserDto(UserModel model) {
            return new UserDto {
                Id = model.Id,
                UserName = model.UserName,
                RealName = model.RealName,
                Role = model.Role,
                IsActive = model.IsActive,
                CreatedTime = model.CreatedTime,
                LastLoginTime = model.LastLoginTime,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };
        }

        /// <summary>
        /// 从DTO创建用户模型
        /// </summary>
        private UserModel CreateUserFromDto(UserCreateDto dto) {
            return new UserModel {
                Id = Guid.NewGuid(),
                UserName = dto.UserName,
                RealName = dto.RealName,
                PinyinCode = CommonHelper.GetPinyinCode(dto.RealName),
                Role = dto.Role,
                IsActive = dto.IsActive,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedTime = DateTime.Now,
                PasswordHash = PasswordHelper.Hash(_options.DefaultUserPassword)
            };
        }

        /// <summary>
        /// 从DTO更新用户模型
        /// </summary>
        private void UpdateUserFromDto(UserModel user, UserDetailDto dto) {
            user.RealName = dto.RealName;
            user.PinyinCode = CommonHelper.GetPinyinCode(dto.RealName);
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
        }

        /// <summary>
        /// 验证用户创建请求
        /// </summary>
        private async Task ValidateUserCreation(UserCreateDto dto) {
            if (await _userRepository.ExistsByUsernameAsync(dto.UserName)) {
                throw new InvalidOperationException("用户名已存在");
            }

            // 单一角色架构下，角色验证已通过Required特性和默认值处理
        }

        /// <summary>
        /// 获取现有用户（不存在时抛出异常）
        /// </summary>
        private async Task<UserModel> GetExistingUser(Guid id) {
            // 内部方法总是包含禁用用户，确保操作能正常进行
            var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
            if (user == null) {
                throw new InvalidOperationException("用户不存在");
            }
            return user;
        }

        /// <summary>
        /// 根据ID列表获取用户列表
        /// </summary>
        private async Task<List<UserModel>> GetUsersByIds(List<Guid> ids) {
            // 内部方法总是包含禁用用户，确保批量操作能正常进行
            return await _userRepository.GetUsersByIdsAsync(ids, includeDisabled: true);
        }

        /// <summary>
        /// 验证批量操作
        /// </summary>
        private void ValidateBatchOperation(List<Guid> ids) {
            if (ids == null || ids.Count == 0) {
                throw new ArgumentException("批量操作的ID列表不能为空");
            }

            if (ids.Count > _options.MaxBatchOperationSize) {
                throw new ArgumentException($"批量操作数量不能超过 {_options.MaxBatchOperationSize}");
            }
        }

        /// <summary>
        /// 统一的用户操作日志记录
        /// </summary>
        private async Task LogUserOperation(
            Guid userId, ActionType actionType, Guid operatorId, string operatorName,
            string content, string? oldValue = null, object? newValue = null) {
            if (!_options.EnableDetailedAuditLogging)
                return;

            await _logService.LogUserActionAsync(
                operatorId,
                operatorName,
                (LogActionType)actionType,
                "Users",
                "UserManagement",
                content,
                parameters: newValue != null ? JsonSerializer.Serialize(newValue) : null
            );
        }

        /// <summary>
        /// 批量操作日志记录
        /// </summary>
        private async Task LogBatchUserOperation(
            List<UserModel> users, ActionType actionType, Guid operatorId, string operatorName,
            string content) {
            if (!_options.EnableDetailedAuditLogging)
                return;

            var userNames = string.Join(", ", users.Select(u => u.UserName));
            var detailedContent = $"{content}: {userNames}";

            await _logService.LogUserActionAsync(
                operatorId,
                operatorName,
                (LogActionType)actionType,
                "Users",
                "BatchUserManagement",
                detailedContent,
                parameters: JsonSerializer.Serialize(users.Select(u => new { u.Id, u.UserName }).ToList())
            );
        }

        /// <summary>
        /// 发送密码重置通知（待实现）
        /// </summary>
        private async Task SendPasswordResetNotification(UserModel user) {
            // TODO: 实现密码重置通知功能
            // 可以发送邮件、短信或系统内通知
            await Task.CompletedTask;
        }

        #endregion 私有辅助方法
    }
}