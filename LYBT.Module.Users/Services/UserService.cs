using LYBT.Common.Enums.Logs;
using System.Collections.Generic;
using System.Linq;
using LYBT.Common.Enums.Users;
using LYBT.Common.Helpers;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Models;
using Microsoft.Extensions.Options;
using LYBT.Module.Users.Interfaces;
using CommonUtil = LYBT.CommonUtils.CommonUtils;

namespace LYBT.Module.Users.Services {

    /// <summary>
    /// 用户服务实现类（集成日志模块）
    /// </summary>
    public class UserService : IUserService {
        private readonly IUserRepository _userRepository;
        private readonly ILogService _logService; // 日志服务
        private readonly UserOptions _options;

        public UserService(IUserRepository userRepository, ILogService logService, IOptions<UserOptions> options) {
            _userRepository = userRepository;
            _logService = logService;
            _options = options.Value;
        }

        /// <summary>
        /// 分页/条件查找用户
        /// </summary>
        public async Task<(IList<UserDto> users, int total)> SearchAsync(UserQueryDto query) {
            var (models, total) = await _userRepository.GetPagedAsync(query);
            var users = new List<UserDto>();
            foreach (var m in models) {
                users.Add(new UserDto {
                    Id = m.Id,
                    UserName = m.UserName,
                    RealName = m.RealName,
                    Role = m.Roles.FirstOrDefault(),
                    Roles = m.Roles,
                    IsActive = m.IsActive,
                    CreatedTime = m.CreatedTime,
                    LastLoginTime = m.LastLoginTime,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber
                });
            }
            return (users, total);
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<UserDto?> GetByIdAsync(Guid id) {
            var m = await _userRepository.GetByIdAsync(id);
            if (m == null)
                return null;
            return new UserDto {
                Id = m.Id,
                UserName = m.UserName,
                RealName = m.RealName,
                Role = m.Roles.FirstOrDefault(),
                Roles = m.Roles,
                IsActive = m.IsActive,
                CreatedTime = m.CreatedTime,
                LastLoginTime = m.LastLoginTime,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber
            };
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<bool> AddAsync(UserCreateDto dto, Guid operatorId, string operatorName) {
            if (await _userRepository.ExistsByUsernameAsync(dto.UserName))
                throw new Exception("用户名已存在");

            var roles = dto.Roles ?? new List<UserRole>();
            var user = new UserModel {
                Id = Guid.NewGuid(),
                UserName = dto.UserName,
                RealName = dto.RealName,
                PinyinCode = CommonUtil.GetPinyinCode(dto.RealName),
                Roles = roles,
                IsActive = dto.IsActive,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                CreatedTime = DateTime.Now,

                PasswordHash = PasswordHelper.Hash(_options.DefaultUserPassword)
            };
            var result = await _userRepository.AddAsync(user);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.User,
                    ObjectId = user.Id,
                    ActionType = ActionType.Create,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"新增用户：{user.UserName}",
                    NewValue = System.Text.Json.JsonSerializer.Serialize(user)
                });
            }
            return result;
        }

        /// <summary>
        /// 编辑用户
        /// </summary>
        public async Task<bool> UpdateAsync(UserDetailDto dto, Guid operatorId, string operatorName) {
            var oldUser = await _userRepository.GetByIdAsync(dto.Id);
            if (oldUser == null)
                throw new Exception("用户不存在");

            // 记录修改前的数据以便日志审计
            var oldSnapshot = System.Text.Json.JsonSerializer.Serialize(oldUser);

            oldUser.RealName = dto.RealName;
            oldUser.PinyinCode = CommonUtil.GetPinyinCode(dto.RealName);
            var roles = dto.Roles ?? new List<UserRole>();
            oldUser.Roles = roles;
            oldUser.IsActive = dto.IsActive;
            oldUser.Email = dto.Email;
            oldUser.PhoneNumber = dto.PhoneNumber;

            var result = await _userRepository.UpdateAsync(oldUser);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.User,
                    ObjectId = oldUser.Id,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"修改用户信息：{oldUser.UserName}",
                    OldValue = oldSnapshot,
                    NewValue = System.Text.Json.JsonSerializer.Serialize(oldUser)
                });
            }
            return result;
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("用户不存在");

            var result = await _userRepository.DisableAsync(id);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.User,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"禁用用户：{user.UserName}",
                    OldValue = System.Text.Json.JsonSerializer.Serialize(user)
                });
            }
            return result;
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("用户不存在");

            var result = await _userRepository.EnableAsync(id);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.User,
                    ObjectId = id,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"启用用户：{user.UserName}",
                    OldValue = System.Text.Json.JsonSerializer.Serialize(user)
                });
            }
            return result;
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var id in ids) {
                if (await DisableAsync(id, operatorId, operatorName))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var id in ids) {
                if (await EnableAsync(id, operatorId, operatorName))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<bool> ResetPasswordAsync(Guid id, Guid operatorId, string operatorName) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("用户不存在");

            user.PasswordHash = PasswordHelper.Hash(_options.DefaultUserPassword);
            var result = await _userRepository.UpdateAsync(user);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.User,
                    ObjectId = id,
                    ActionType = ActionType.ResetPassword,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"重置用户密码：{user.UserName}"
                });
            }
            return result;
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return false;
            if (!PasswordHelper.Verify(user.PasswordHash, oldPassword))
                return false;
            user.PasswordHash = PasswordHelper.Hash(newPassword);
            return await _userRepository.UpdateAsync(user);
        }

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        public async Task<bool> ChangeProfileAsync(Guid id, string realName, string? email, string? phoneNumber) {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return false;
            user.RealName = realName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            return await _userRepository.UpdateAsync(user);
        }

/// <summary>
/// 执行GetRoles操作。
/// </summary>
/// <returns>返回值</returns>
        public List<UserRole> GetRoles() {
            return Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
        }
    }
}
