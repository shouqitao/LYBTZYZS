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
    /// 统一用户服务 - 合并 BusinessService 和 QueryService
    /// 职责：用户的完整业务逻辑和查询功能
    /// </summary>
    public partial class UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger,
        IOptions<UserOptions> options,
        DefaultPasswordService defaultPasswordService) : IUserService
    {
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly ILogger<UserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        #endregion

        #region Query Operations - 来自原 UserQueryService

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");
                }

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败，用户ID: {UserId}", id);
                return ServiceResult<UserDto>.Failure("获取用户详情失败");
            }
        }

        /// <summary>
        /// 分页获取用户列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query)
        {
            try
            {
                if (query.PageSize <= 0 || query.PageSize > 100)
                {
                    query.PageSize = 20; // 默认页大小
                }

                if (query.PageIndex < 1)
                {
                    query.PageIndex = 1;
                }

                var result = await _userRepository.GetPagedAsync(query);
                var dtos = _mapper.Map<List<UserDto>>(result.Items);

                // 使用构造函数创建PagedResult
                var pagedResult = new PagedResult<UserDto>(dtos, result.TotalCount, query.PageIndex, query.PageSize);

                return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取用户列表失败");
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var users = await _userRepository.SearchAsync(keyword, null, null, 50);
                var dtos = _mapper.Map<List<UserDto>>(users);

                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败，关键词: {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure("搜索用户失败");
            }
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<UserDto>.Failure("用户名不能为空");
                }

                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败，用户名: {Username}", username);
                return ServiceResult<UserDto>.Failure("获取用户失败");
            }
        }

        /// <summary>
        /// 根据邮箱获取用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ServiceResult<UserDto>.Failure("邮箱不能为空");
                }

                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                var dto = _mapper.Map<UserDto>(user);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据邮箱获取用户失败，邮箱: {Email}", email);
                return ServiceResult<UserDto>.Failure("获取用户失败");
            }
        }

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> IsUsernameExistsAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return ServiceResult<bool>.Failure("用户名不能为空");
                }

                var exists = await _userRepository.IsUsernameExistsAsync(username);
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户名是否存在失败，用户名: {Username}", username);
                return ServiceResult<bool>.Failure("检查用户名失败");
            }
        }

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> IsEmailExistsAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ServiceResult<bool>.Failure("邮箱不能为空");
                }

                var exists = await _userRepository.IsEmailExistsAsync(email);
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查邮箱是否存在失败，邮箱: {Email}", email);
                return ServiceResult<bool>.Failure("检查邮箱失败");
            }
        }

        /// <summary>
        /// 获取角色用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role)
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync(role);
                var dtos = _mapper.Map<List<UserDto>>(users);

                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色用户列表失败，角色: {Role}", role);
                return ServiceResult<List<UserDto>>.Failure("获取角色用户列表失败");
            }
        }

        /// <summary>
        /// 获取在线用户数量
        /// </summary>
        public async Task<ServiceResult<int>> GetOnlineCountAsync()
        {
            try
            {
                var count = await _userRepository.GetOnlineCountAsync();
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取在线用户数量失败");
                return ServiceResult<int>.Failure("获取在线用户数量失败");
            }
        }

        #endregion

        #region Business Operations - 来自原 UserBusinessService

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto createUserDto)
        {
            try
            {
                // 验证输入
                var validationResult = await ValidateCreateUserInputAsync(createUserDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.Message);
                }

                // 检查用户名和邮箱唯一性
                var uniquenessResult = await ValidateUsernameAndEmailUniquenessAsync(createUserDto.Username, createUserDto.Email);
                if (!uniquenessResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(uniquenessResult.Message);
                }

                // 创建用户实体
                var user = CreateUserEntity(createUserDto);

                // 保存到数据库（使用AddAsync而非CreateAsync）
                var createdUser = await _userRepository.AddAsync(user);
                var dto = _mapper.Map<UserDto>(createdUser);

                _logger.LogInformation("成功创建用户: {Username}", createUserDto.Username);
                return ServiceResult<UserDto>.Success(dto, "用户创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败，用户名: {Username}", createUserDto?.Username);
                return ServiceResult<UserDto>.Failure("创建用户失败");
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto updateUserDto)
        {
            try
            {
                // 验证输入
                var validationResult = await ValidateUpdateUserInputAsync(id, updateUserDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<UserDto>.Failure(validationResult.Message);
                }

                // 获取现有用户
                var existingUser = await _userRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                // 检查邮箱唯一性（如果邮箱发生变化）
                if (!string.IsNullOrEmpty(updateUserDto.Email) &&
                    updateUserDto.Email != existingUser.Email)
                {
                    var emailExists = await _userRepository.IsEmailExistsAsync(updateUserDto.Email);
                    if (emailExists)
                    {
                        return ServiceResult<UserDto>.Failure("该邮箱已被其他用户使用");
                    }
                }

                // 更新用户信息
                UpdateUserEntity(existingUser, updateUserDto);

                // 保存更改
                var updatedUser = await _userRepository.UpdateAsync(existingUser);
                var dto = _mapper.Map<UserDto>(updatedUser);

                _logger.LogInformation("成功更新用户信息，用户ID: {UserId}", id);
                return ServiceResult<UserDto>.Success(dto, "用户信息更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败，用户ID: {UserId}", id);
                return ServiceResult<UserDto>.Failure("更新用户信息失败");
            }
        }

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

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                if (user.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<bool>.Failure("用户已被禁用");
                }

                user.Status = CommonStatus.Disabled;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("成功禁用用户，用户ID: {UserId}", id);
                return ServiceResult<bool>.Success(true, "用户已成功禁用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败，用户ID: {UserId}", id);
                return ServiceResult<bool>.Failure("禁用用户失败");
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

                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                if (user.Status == CommonStatus.Enabled)
                {
                    return ServiceResult<bool>.Failure("用户已启用");
                }

                user.Status = CommonStatus.Enabled;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("成功启用用户，用户ID: {UserId}", id);
                return ServiceResult<bool>.Success(true, "用户已成功启用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败，用户ID: {UserId}", id);
                return ServiceResult<bool>.Failure("启用用户失败");
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            // 方法已删除 - 依赖PasswordHelper类不存在
            return ServiceResult<bool>.Failure("密码重置功能暂未实现");
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            // 方法已删除 - 依赖PasswordHelper类不存在
            return ServiceResult<bool>.Failure("密码修改功能暂未实现");
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> userIds)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                {
                    return ServiceResult<int>.Failure("用户ID列表不能为空");
                }

                var successCount = 0;

                foreach (var userId in userIds)
                {
                    try
                    {
                        var disableResult = await DisableAsync(userId);
                        if (disableResult.IsSuccess)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "批量禁用用户时发生错误，用户ID: {UserId}", userId);
                    }
                }

                _logger.LogInformation("批量禁用用户操作完成，成功: {SuccessCount}", successCount);

                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败");
                return ServiceResult<int>.Failure("批量禁用操作失败");
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<ServiceResult> ValidateCreateUserInputAsync(UserCreateDto createUserDto)
        {
            if (createUserDto == null)
                return ServiceResult.Failure("用户信息不能为空");

            if (string.IsNullOrWhiteSpace(createUserDto.Username))
                return ServiceResult.Failure("用户名不能为空");


            if (!UsernameValidationRegex().IsMatch(createUserDto.Username))
                return ServiceResult.Failure("用户名只能包含字母、数字和下划线");

            if (string.IsNullOrWhiteSpace(createUserDto.Email))
                return ServiceResult.Failure("邮箱不能为空");

            if (!EmailValidationRegex().IsMatch(createUserDto.Email))
                return ServiceResult.Failure("邮箱格式不正确");

            if (!string.IsNullOrEmpty(createUserDto.PhoneNumber) &&
                !PhoneValidationRegex().IsMatch(createUserDto.PhoneNumber))
                return ServiceResult.Failure("手机号格式不正确");

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateUpdateUserInputAsync(Guid id, UserUpdateDto updateUserDto)
        {
            if (id == Guid.Empty)
                return ServiceResult.Failure("用户ID不能为空");

            if (updateUserDto == null)
                return ServiceResult.Failure("更新信息不能为空");

            if (!string.IsNullOrEmpty(updateUserDto.Email) &&
                !EmailValidationRegex().IsMatch(updateUserDto.Email))
                return ServiceResult.Failure("邮箱格式不正确");

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber) &&
                !PhoneValidationRegex().IsMatch(updateUserDto.PhoneNumber))
                return ServiceResult.Failure("手机号格式不正确");

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateUsernameAndEmailUniquenessAsync(string username, string email)
        {
            var usernameExists = await _userRepository.IsUsernameExistsAsync(username);
            if (usernameExists)
                return ServiceResult.Failure("用户名已存在");

            var emailExists = await _userRepository.IsEmailExistsAsync(email);
            if (emailExists)
                return ServiceResult.Failure("邮箱已存在");

            return ServiceResult.Success();
        }

        private static ServiceResult ValidatePasswordChangeInput(Guid id, string oldPassword, string newPassword)
        {
            if (id == Guid.Empty)
                return ServiceResult.Failure("用户ID不能为空");

            if (string.IsNullOrWhiteSpace(oldPassword))
                return ServiceResult.Failure("原密码不能为空");

            if (string.IsNullOrWhiteSpace(newPassword))
                return ServiceResult.Failure("新密码不能为空");

            if (newPassword.Length < 6 || newPassword.Length > 50)
                return ServiceResult.Failure("新密码长度必须在6-50个字符之间");

            return ServiceResult.Success();
        }

        private LYBT.Entities.Users.User CreateUserEntity(UserCreateDto createUserDto)
        {
            var now = DateTime.UtcNow;


            return new LYBT.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                Username = createUserDto.Username,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                Role = createUserDto.Role,

                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private static void UpdateUserEntity(LYBT.Entities.Users.User user, UserUpdateDto updateUserDto)
        {
            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;

            if (updateUserDto.Role.HasValue)
                user.Role = updateUserDto.Role.Value;

            user.UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region Additional Interface Methods

        /// <summary>
        /// 获取启用的用户列表
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var users = await _userRepository.SearchAsync(null, null, CommonStatus.Enabled, 100);
                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取启用用户列表失败");
                return ServiceResult<List<UserDto>>.Failure("获取启用用户列表失败");
            }
        }

        /// <summary>
        /// 获取系统所有角色
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                var roles = Enum.GetValues<UserRole>()
                    .Select(r => new { Value = (int)r, Text = r.ToString() })
                    .Cast<object>()
                    .ToList();
                return ServiceResult<List<object>>.Success(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表失败");
                return ServiceResult<List<object>>.Failure("获取角色列表失败");
            }
        }

        /// <summary>
        /// 验证用户名是否可用
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return ServiceResult<bool>.Failure("用户名不能为空");
                }

                var exists = await _userRepository.IsUsernameExistsAsync(userName);
                return ServiceResult<bool>.Success(!exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户名失败: {UserName}", userName);
                return ServiceResult<bool>.Failure("验证用户名失败");
            }
        }

        /// <summary>
        /// 获取所有医生
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> GetDoctorsAsync()
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync(UserRole.Doctor);
                var dtos = _mapper.Map<List<UserDto>>(users);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生列表失败");
                return ServiceResult<List<UserDto>>.Failure("获取医生列表失败");
            }
        }

        /// <summary>
        /// 检查医生是否在线
        /// </summary>
        public async Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(doctorId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                if (user.Role != UserRole.Doctor)
                {
                    return ServiceResult<bool>.Failure("用户不是医生");
                }

                var isAvailable = user.Status == CommonStatus.Enabled;
                return ServiceResult<bool>.Success(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医生可用性失败: {DoctorId}", doctorId);
                return ServiceResult<bool>.Failure("检查医生可用性失败");
            }
        }

        /// <summary>
        /// 创建用户（带取消令牌）
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            return await CreateUserAsync(dto);
        }

        /// <summary>
        /// 更新用户（带取消令牌）
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            return await UpdateUserAsync(id, dto);
        }

        /// <summary>
        /// 删除用户（软删除）
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

                user.IsDeleted = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("成功删除用户，用户ID: {UserId}", id);
                return ServiceResult<bool>.Success(true, "用户已成功删除");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败，用户ID: {UserId}", id);
                return ServiceResult<bool>.Failure("删除用户失败");
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

                var successCount = 0;

                foreach (var userId in ids)
                {
                    try
                    {
                        var enableResult = await EnableAsync(userId);
                        if (enableResult.IsSuccess)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "批量启用用户时发生错误，用户ID: {UserId}", userId);
                    }
                }

                _logger.LogInformation("批量启用用户操作完成，成功: {SuccessCount}", successCount);

                return ServiceResult<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败");
                return ServiceResult<int>.Failure("批量启用操作失败");
            }
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ServiceResult<bool>.Failure("用户不存在");
                }

                // 更新基本信息（移除不存在的DisplayName属性）
                user.RealName = realName;
                user.PhoneNumber = phoneNumber;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true, "个人信息更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人信息失败，用户ID: {UserId}", userId);
                return ServiceResult<bool>.Failure("修改个人信息失败");
            }
        }

        #endregion
    }
}
