using System.Text.RegularExpressions;
using AutoMapper;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Users.Services
{

    /// <summary>
    /// 用户业务服务 - UltraThink架构
    /// 职责：业务逻辑，状态管理，密码管理，批量操作
    /// </summary>
    public partial class UserBusinessService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserBusinessService> logger,
        IOptions<UserOptions> options,
        DefaultPasswordService defaultPasswordService) : IUserBusinessService
    {
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly ILogger<UserBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly UserOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly DefaultPasswordService _defaultPasswordService = defaultPasswordService ?? throw new ArgumentNullException(nameof(defaultPasswordService));

        #region 生成的正则表达式 - SYSLIB1045 优化

        /// <summary>
        /// 用户名验证正则表达式 - 只允许字母、数字、下划线
        /// </summary>
        [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
        private static partial Regex UsernameValidationRegex();

        /// <summary>
        /// 邮箱验证正则表达式
        /// </summary>
        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
        private static partial Regex EmailValidationRegex();

        /// <summary>
        /// 手机号验证正则表达式 - 中国手机号格式
        /// </summary>
        [GeneratedRegex(@"^1[3-9]\d{9}$")]
        private static partial Regex PhoneValidationRegex();

        #endregion 生成的正则表达式 - SYSLIB1045 优化

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                var result = await _userRepository.DisableAsync(id);
                if (result)
                {
                    var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
                    _logger.LogInformation("禁用用户成功: {Username} ({Id})", user?.Username, user?.Id);
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    return ServiceResult<bool>.Failure("禁用用户失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                var result = await _userRepository.EnableAsync(id);
                if (result)
                {
                    var user = await _userRepository.GetByIdAsync(id);
                    _logger.LogInformation("启用用户成功: {Username} ({Id})", user?.Username, user?.Id);
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    return ServiceResult<bool>.Failure("启用用户失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }

                var validIds = ids.Where(id => id != Guid.Empty).ToList();
                if (validIds.Count == 0)
                {
                    return ServiceResult<int>.Failure("没有有效的用户ID");
                }

                var affectedRows = await _userRepository.UpdateActiveStatusAsync(validIds, false);

                _logger.LogInformation("批量禁用用户成功，影响行数: {Count}", affectedRows);
                return ServiceResult<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败");
                return ServiceResult<int>.Failure($"批量禁用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }

                var validIds = ids.Where(id => id != Guid.Empty).ToList();
                if (validIds.Count == 0)
                {
                    return ServiceResult<int>.Failure("没有有效的用户ID");
                }

                var affectedRows = await _userRepository.UpdateActiveStatusAsync(validIds, true);

                _logger.LogInformation("批量启用用户成功，影响行数: {Count}", affectedRows);
                return ServiceResult<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败");
                return ServiceResult<int>.Failure($"批量启用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult<bool>.Failure("新密码不能为空");
                }

                // 验证密码复杂度
                if (!Shared.Utilities.Security.PasswordPolicyValidator.Validate(newPassword, out var errors))
                {
                    return ServiceResult<bool>.Failure($"密码不符合复杂度要求：{string.Join("；", errors)}");
                }

                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);

                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                // 更新密码哈希
                var passwordHash = PasswordHelper.Hash(newPassword);
                var updateResult = await _userRepository.UpdatePasswordAsync(id, passwordHash);
                if (!updateResult)
                {
                    return ServiceResult<bool>.Failure("密码更新失败");
                }

                _logger.LogInformation("重置用户密码成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败: {Id}", id);
                return ServiceResult<bool>.Failure($"重置密码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    return ServiceResult<bool>.Failure("原密码不能为空");
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult<bool>.Failure("新密码不能为空");
                }

                // 验证密码复杂度
                if (!Shared.Utilities.Security.PasswordPolicyValidator.Validate(newPassword, out var errors))
                {
                    return ServiceResult<bool>.Failure($"密码不符合复杂度要求：{string.Join("；", errors)}");
                }

                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);

                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                // 验证原密码
                if (!PasswordHelper.Verify(user.PasswordHash, oldPassword))
                {
                    return ServiceResult<bool>.Failure("原密码错误");
                }

                // 更新密码哈希
                var passwordHash = PasswordHelper.Hash(newPassword);
                var updateResult = await _userRepository.UpdatePasswordAsync(id, passwordHash);
                if (!updateResult)
                {
                    return ServiceResult<bool>.Failure("密码更新失败");
                }

                _logger.LogInformation("用户修改密码成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户修改密码失败: {Id}", id);
                return ServiceResult<bool>.Failure($"修改密码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(realName))
                {
                    return ServiceResult<bool>.Failure("真实姓名不能为空");
                }

                var user = await _userRepository.GetByIdAsync(userId, includeDisabled: true);

                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                // 更新个人信息
                user.RealName = realName;
                user.PhoneNumber = phoneNumber;
                user.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("用户修改个人信息成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人信息失败: {UserId}", userId);
                return ServiceResult<bool>.Failure($"修改个人信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建用户业务逻辑
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 业务规则验证
                var validationResult = await ValidateUserCreationAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage ?? "用户数据验证失败");
                }

                // 检查用户名是否重复
                var userExists = await _userRepository.ExistsByUsernameAsync(dto.Username);
                if (userExists)
                {
                    return ServiceResult<UserDto>.Failure("用户名已存在");
                }

                var user = new Entities.Users.User
                {
                    Id = Guid.NewGuid(),
                    Username = dto.Username,
                    PasswordHash = PasswordHelper.Hash(dto.Password ?? _defaultPasswordService.GetNewUserPassword() ?? "LybtUser2025#InitPass!"),
                    RealName = dto.RealName,
                    Role = dto.Role,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Status = dto.Status,
                    PinYinCode = string.Empty, // 移除CommonHelper依赖，拼音码功能暂不实现

                };

                var createdUser = await _userRepository.AddAsync(user);

                _logger.LogInformation("创建用户成功: {Username} ({Id})", createdUser.Username, createdUser.Id);

                var resultDto = _mapper.Map<UserDto>(createdUser);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: Username {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户业务逻辑
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }

                // 业务规则验证
                var validationResult = await ValidateUserUpdateAsync(dto, id);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage ?? "用户数据验证失败");
                }

                var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);

                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                // 更新字段
                if (!string.IsNullOrEmpty(dto.RealName))
                    user.RealName = dto.RealName;
                if (dto.Role.HasValue)
                    user.Role = dto.Role.Value;
                user.PhoneNumber = dto.PhoneNumber;
                user.Email = dto.Email;
                user.Status = dto.Status;
                user.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现

                var updatedUser = await _userRepository.UpdateAsync(user);

                _logger.LogInformation("更新用户成功: {Username} ({Id})", updatedUser.Username, updatedUser.Id);

                var resultDto = _mapper.Map<UserDto>(updatedUser);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败: {Id}", id);
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除用户业务逻辑
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("用户ID不能为空");
                }

                var user = await _userRepository.GetByIdAsync(id);

                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                // 业务规则：检查是否可以删除
                if (user.Role == UserRole.Admin)
                {
                    var activeUsers = await _userRepository.GetActiveUsersAsync();
                    var adminCount = activeUsers.Count(u => u.Role == UserRole.Admin);
                    if (adminCount <= 1)
                    {
                        return ServiceResult<bool>.Failure("至少需要保留一个管理员用户");
                    }
                }

                // 软删除 - 使用Repository的DisableAsync方法
                var result = await _userRepository.DisableAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("删除用户失败");
                }

                _logger.LogInformation("删除用户成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 统一用户变更DTO验证 - UltraThink现代化DTO设计
        /// </summary>
        private static async Task<ServiceResult<bool>> ValidateUserCreationAsync(UserCreateDto dto)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("用户信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return ServiceResult<bool>.Failure("用户名不能为空");
            }

            if (dto.Username.Length < 3 || dto.Username.Length > 50)
            {
                return ServiceResult<bool>.Failure("用户名长度必须在3-50字符之间");
            }

            // 检查用户名格式（只能包含字母、数字、下划线）- 使用生成的正则表达式
            if (!UsernameValidationRegex().IsMatch(dto.Username))
            {
                return ServiceResult<bool>.Failure("用户名只能包含字母、数字和下划线");
            }

            if (string.IsNullOrWhiteSpace(dto.RealName))
            {
                return ServiceResult<bool>.Failure("真实姓名不能为空");
            }

            // 邮箱格式验证（如果提供）- 使用生成的正则表达式
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (!EmailValidationRegex().IsMatch(dto.Email))
                {
                    return ServiceResult<bool>.Failure("邮箱格式不正确");
                }
            }

            // 手机号格式验证（如果提供）- 使用生成的正则表达式
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                if (!PhoneValidationRegex().IsMatch(dto.PhoneNumber))
                {
                    return ServiceResult<bool>.Failure("手机号格式不正确");
                }
            }

            await Task.CompletedTask; // 保持异步签名
            return ServiceResult<bool>.Success(true);
        }

        private static async Task<ServiceResult<bool>> ValidateUserUpdateAsync(UserUpdateDto dto, Guid existingUserId)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("用户信息不能为空");
            }

            if (existingUserId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("用户ID无效");
            }

            if (string.IsNullOrWhiteSpace(dto.RealName))
            {
                return ServiceResult<bool>.Failure("真实姓名不能为空");
            }

            // 邮箱格式验证（如果提供）- 使用生成的正则表达式
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                if (!EmailValidationRegex().IsMatch(dto.Email))
                {
                    return ServiceResult<bool>.Failure("邮箱格式不正确");
                }
            }

            // 手机号格式验证（如果提供）- 使用生成的正则表达式
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                if (!PhoneValidationRegex().IsMatch(dto.PhoneNumber))
                {
                    return ServiceResult<bool>.Failure("手机号格式不正确");
                }
            }

            await Task.CompletedTask; // 保持异步签名
            return ServiceResult<bool>.Success(true);
        }


        #endregion 私有方法
    }
}
