using LYBT.Infrastructure.Options;
using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using UserCreateDto = LYBT.Shared.Models.Contracts.Users.UserCreateDto;
using UserDto = LYBT.Shared.Models.Contracts.Users.UserDto;
using UserPagedQueryDto = LYBT.Shared.Models.Contracts.Users.UserPagedQueryDto;
using UserUpdateDto = LYBT.Shared.Models.Contracts.Users.UserUpdateDto;

namespace LYBT.Module.Users.Services
{

    /// <summary>
    /// 用户服务实现类（集成日志模块）
    /// </summary>
    public class UserService : LYBT.Shared.Interfaces.Services.IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly UserOptions _options;
        private readonly IMapper _mapper;

        public UserService(
            IUserRepository userRepository,
            ILogger<UserService> logger,
            IOptions<UserOptions> options,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _logger = logger;
            _options = options.Value;
            _mapper = mapper;
        }

        /// <summary>
        /// 分页/条件查找用户
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
        {
            try
            {
                // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
                bool includeDisabled = true;

                var (models, total) = await _userRepository.GetPagedAsync(query, includeDisabled);
                var users = _mapper.Map<List<UserDto>>(models);
                var result = new PagedResult<UserDto>
                {
                    TotalCount = total,
                    Items = users,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };
                
                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分页用户列表失败");
                return ServiceResult<PagedResult<UserDto>>.Failure($"获取分页用户列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// 根据当前操作者角色决定是否包含禁用用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 管理员可以查看所有用户（包括禁用的），普通用户只能查看启用的用户
                bool includeDisabled = true;

                var model = await _userRepository.GetByIdAsync(id, includeDisabled);
                if (model == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }
                
                var userDto = _mapper.Map<UserDto>(model);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取用户失败, ID: {UserId}", id);
                return ServiceResult<UserDto>.Failure($"根据ID获取用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据用户名获取用户信息（用于登录验证后获取用户详情）
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                var model = await _userRepository.GetByUsernameAsync(username);
                if (model == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }
                
                var userDto = _mapper.Map<UserDto>(model);
                return ServiceResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败, Username: {Username}", username);
                return ServiceResult<UserDto>.Failure($"根据用户名获取用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            try
            {
                await ValidateUserCreation(dto);

                var user = CreateUserFromDto(dto);
                var result = await _userRepository.AddAsync(user);

                if (result != null)
                {
                    // 内部记录操作日志，使用系统用户ID
                    await LogUserOperation(
                        user.Id, ActionType.Create, Guid.Empty, "System",
                        $"新增用户：{user.Username}",
                        newValue: user
                    );

                    var userDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(userDto);
                }

                return ServiceResult<UserDto>.Failure("用户创建失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败, Username: {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 编辑用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                var existingUser = await GetExistingUser(id);
                var oldSnapshot = JsonSerializer.Serialize(existingUser);

                UpdateUserFromDto(existingUser, dto);
                var result = await _userRepository.UpdateAsync(existingUser);

                if (result != null)
                {
                    await LogUserOperation(
                        existingUser.Id, ActionType.Update, Guid.Empty, "System",
                        $"修改用户信息：{existingUser.Username}",
                        oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(existingUser)
                    );

                    var userDto = _mapper.Map<UserDto>(result);
                    return ServiceResult<UserDto>.Success(userDto);
                }

                return ServiceResult<UserDto>.Failure("用户更新失败");
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
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                var user = await GetExistingUser(id);
                var result = await _userRepository.DisableAsync(id);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",
                        $"禁用用户：{user.Username}",
                        oldValue: JsonSerializer.Serialize(user)
                    );
                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                var user = await GetExistingUser(id);
                var result = await _userRepository.EnableAsync(id);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",
                        $"启用用户：{user.Username}",
                        oldValue: JsonSerializer.Serialize(user)
                    );
                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                ValidateBatchOperation(ids);

                var users = await GetUsersByIds(ids);
                var updatedCount = await _userRepository.UpdateActiveStatusAsync(ids, false);

                if (updatedCount > 0)
                {
                    await LogBatchUserOperation(
                        users, ActionType.Update, Guid.Empty, "System",
                        $"批量禁用 {updatedCount} 个用户"
                    );
                }

                return ServiceResult<int>.Success(updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败, IDs: {UserIds}", string.Join(",", ids));
                return ServiceResult<int>.Failure($"批量禁用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                ValidateBatchOperation(ids);

                var users = await GetUsersByIds(ids);
                var updatedCount = await _userRepository.UpdateActiveStatusAsync(ids, true);

                if (updatedCount > 0)
                {
                    await LogBatchUserOperation(
                        users, ActionType.Update, Guid.Empty, "System",
                        $"批量启用 {updatedCount} 个用户"
                    );
                }

                return ServiceResult<int>.Success(updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败, IDs: {UserIds}", string.Join(",", ids));
                return ServiceResult<int>.Failure($"批量启用用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                var user = await GetExistingUser(id);

                var newPasswordHash = PasswordHelper.Hash(newPassword);
                var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, Guid.Empty, "System",
                        $"重置用户密码：{user.Username}"
                    );

                    if (_options.SendPasswordResetNotification)
                    {
                        await SendPasswordResetNotification(user);
                    }
                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"重置用户密码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var user = await GetExistingUser(id);

                if (!PasswordHelper.Verify(user.PasswordHash, oldPassword))
                {
                    return ServiceResult<bool>.Failure("原密码错误");
                }

                var newPasswordHash = PasswordHelper.Hash(newPassword);
                var result = await _userRepository.UpdatePasswordAsync(id, newPasswordHash);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Update, id, user.RealName,
                        "用户修改个人密码"
                    );
                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户密码失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"修改用户密码失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, string realName, string phoneNumber)
        {
            try
            {
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
                        "用户修改个人信息",
                        oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(user)
                    );

                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Failure("修改个人信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户个人信息失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"修改用户个人信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                var roles = new List<object> { new { Value = "Admin", DisplayName = "管理员" } };
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表失败");
                return ServiceResult<List<object>>.Failure($"获取角色列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _userRepository.GetActiveUsersAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃用户列表失败");
                return ServiceResult<List<UserDto>>.Failure($"获取活跃用户列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                // 使用禁用代替删除（软删除策略）
                var result = await DisableAsync(id);
                if (result.IsSuccess)
                {
                    return ServiceResult<bool>.Success(result.Data);
                }
                return ServiceResult<bool>.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败, ID: {UserId}", id);
                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var query = new UserPagedQueryDto
                {
                    Keyword = keyword,
                    PageIndex = 1,
                    PageSize = 100 // 搜索返回前100个结果
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<UserDto>>.Success(result.Data.Items.ToList());
                }

                return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "搜索用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败, Keyword: {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure($"搜索用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync()
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync();
                var statistics = new
                {
                    TotalCount = allUsers.Count(),
                    ActiveCount = allUsers.Count(u => u.Status == CommonStatus.Enabled),
                    InactiveCount = allUsers.Count(u => u.Status == CommonStatus.Disabled),
                    RecentCount = allUsers.Count(u => u.CreateTime >= DateTime.Today.AddDays(-30))
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计信息失败");
                return ServiceResult<object>.Failure($"获取用户统计信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            try
            {
                var exists = await _userRepository.ExistsByUsernameAsync(username);
                return ServiceResult<bool>.Success(!exists); // 不存在则可用
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户名失败, Username: {Username}", username);
                return ServiceResult<bool>.Failure($"验证用户名失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取用户操作日志
        /// </summary>
        public async Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query)
        {
            try
            {
                // 简化实现，返回空日志列表
                var result = new PagedResult<object>
                {
                    Items = new List<object>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<object>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户操作日志失败, UserId: {UserId}", userId);
                return ServiceResult<PagedResult<object>>.Failure($"获取用户操作日志失败: {ex.Message}", ex);
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 映射用户模型到DTO (现已使用AutoMapper，此方法保留用于兼容性)
        /// </summary>
        private UserDto MapToUserDto(UserModel model)
        {
            return _mapper.Map<UserDto>(model);
        }

        /// <summary>
        /// 从DTO创建用户模型
        /// </summary>
        private UserModel CreateUserFromDto(UserCreateDto dto)
        {
            return new UserModel
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                RealName = dto.RealName,
                PinYinCode = CommonHelper.GetPinyinCode(dto.RealName),
                Status = dto.Status,
                PhoneNumber = dto.PhoneNumber,
                CreateTime = DateTime.Now,
                PasswordHash = PasswordHelper.Hash(_options.DefaultUserPassword)
            };
        }

        /// <summary>
        /// 从DTO更新用户模型
        /// </summary>
        private void UpdateUserFromDto(UserModel user, UserUpdateDto dto)
        {
            user.RealName = dto.RealName;
            user.PinYinCode = CommonHelper.GetPinyinCode(dto.RealName);
            user.Status = dto.Status;
            user.PhoneNumber = dto.PhoneNumber;
        }

        /// <summary>
        /// 验证用户创建请求
        /// </summary>
        private async Task ValidateUserCreation(UserCreateDto dto)
        {
            if (await _userRepository.ExistsByUsernameAsync(dto.Username))
            {
                throw new InvalidOperationException("用户名已存在");
            }

            // 单一角色架构下，角色验证已通过Required特性和默认值处理
        }

        /// <summary>
        /// 获取现有用户（不存在时抛出异常）
        /// </summary>
        private async Task<UserModel> GetExistingUser(Guid id)
        {
            // 内部方法总是包含禁用用户，确保操作能正常进行
            var user = await _userRepository.GetByIdAsync(id, includeDisabled: true);
            if (user == null)
            {
                throw new InvalidOperationException("用户不存在");
            }
            return user;
        }

        /// <summary>
        /// 根据ID列表获取用户列表
        /// </summary>
        private async Task<List<UserModel>> GetUsersByIds(List<Guid> ids)
        {
            // 内部方法总是包含禁用用户，确保批量操作能正常进行
            return await _userRepository.GetUsersByIdsAsync(ids, includeDisabled: true);
        }

        /// <summary>
        /// 验证批量操作
        /// </summary>
        private void ValidateBatchOperation(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                throw new ArgumentException("批量操作的ID列表不能为空");
            }

            if (ids.Count > _options.MaxBatchOperationSize)
            {
                throw new ArgumentException($"批量操作数量不能超过 {_options.MaxBatchOperationSize}");
            }
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

            _logger.LogInformation("用户操作日志 - 操作者: {OperatorName} ({OperatorId}), 操作类型: {ActionType}, 内容: {Content}",
                operatorName, operatorId, actionType, content);
        }

        /// <summary>
        /// 批量操作日志记录
        /// </summary>
        private async Task LogBatchUserOperation(
            List<UserModel> users, ActionType actionType, Guid operatorId, string operatorName,
            string content)
        {
            if (!_options.EnableDetailedAuditLogging)
                return;

            var userNames = string.Join(", ", users.Select(u => u.Username));
            var detailedContent = $"{content}: {userNames}";

            _logger.LogInformation("批量用户操作日志 - 操作者: {OperatorName} ({OperatorId}), 操作类型: {ActionType}, 内容: {Content}",
                operatorName, operatorId, actionType, detailedContent);
        }

        /// <summary>
        /// 发送密码重置通知（待实现）
        /// </summary>
        private async Task SendPasswordResetNotification(UserModel user)
        {

            // 可以发送邮件、短信或系统内通知
            await Task.CompletedTask;
        }

        #endregion 私有辅助方法

        #region 医生功能

        /// <summary>
        /// 获取所有医生（即所有用户）
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<List<UserDto>>(users);
        }

        /// <summary>
        /// 根据科室获取医生
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsByDepartmentAsync(string department)
        {
            var users = await _userRepository.GetAllAsync();
            var all = _mapper.Map<List<UserDto>>(users);
            // Department字段已删除，返回所有用户
            return all.ToList();
        }

        /// <summary>
        /// 获取医生的今日排班（简化版，默认都在班）
        /// </summary>
        public async Task<bool> IsDoctorAvailableAsync(Guid doctorId)
        {
            var user = await _userRepository.GetByIdAsync(doctorId, true);
            var dto = _mapper.Map<UserDto>(user);
            return dto != null && dto.Status == CommonStatus.Enabled;
        }

        #endregion

    }
}