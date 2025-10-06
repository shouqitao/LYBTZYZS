using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 用户服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// Desktop Client简化实现，部分业务逻辑委托给Repository
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public UserService(
            IUserRepository repository,
            ILogger<UserService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        #region 查询操作

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    allUsers = allUsers.Where(u =>
                        u.UserName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.RealName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 分页
                var totalCount = allUsers.Count;
                var items = allUsers
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                var pagedResult = new PagedResult<UserDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                return ServiceResult<UserDto>.Success(user);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var user = allUsers.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                return ServiceResult<UserDto>.Success(user);
            }, nameof(GetByUsernameAsync));
        }

        public async Task<ServiceResult<UserDto>> GetByEmailAsync(string email)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var user = allUsers.FirstOrDefault(u => u.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true);

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                return ServiceResult<UserDto>.Success(user);
            }, nameof(GetByEmailAsync));
        }

        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var activeUsers = allUsers.Where(u => u.Status == CommonStatus.Enabled).ToList();
                return ServiceResult<List<UserDto>>.Success(activeUsers);
            }, nameof(GetActiveUsersAsync));
        }

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"搜索用户: {keyword}");

                var allUsers = await _repository.GetAllAsync();
                var results = allUsers.Where(u =>
                    u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return ServiceResult<List<UserDto>>.Success(results);
            }, nameof(SearchAsync));
        }

        public async Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 返回UserRole枚举的所有值
                var roles = Enum.GetValues(typeof(UserRole))
                    .Cast<UserRole>()
                    .Select(r => new { Value = (int)r, Name = r.ToString() } as object)
                    .ToList();

                return await Task.FromResult(ServiceResult<List<object>>.Success(roles));
            }, nameof(GetRolesAsync));
        }

        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string userName)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var exists = allUsers.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
                return ServiceResult<bool>.Success(!exists);
            }, nameof(ValidateUsernameAsync));
        }

        public async Task<ServiceResult<List<UserDto>>> GetDoctorsAsync()
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var doctors = allUsers.Where(u => u.Role == UserRole.Doctor).ToList();
                return ServiceResult<List<UserDto>>.Success(doctors);
            }, nameof(GetDoctorsAsync));
        }

        public async Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var doctor = await _repository.GetByIdAsync(doctorId);
                var isAvailable = doctor != null && doctor.Status == CommonStatus.Enabled;
                return ServiceResult<bool>.Success(isAvailable);
            }, nameof(IsDoctorAvailableAsync));
        }

        #endregion

        #region 认证操作

        public async Task<ServiceResult<bool>> ValidatePasswordAsync(Guid userId, string password)
        {
            // Desktop Client不实现密码验证逻辑，应该由Server端处理
            return await Task.FromResult(ServiceResult<bool>.Failure("Desktop Client不支持密码验证"));
        }

        public async Task<ServiceResult<UserDto>> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allUsers = await _repository.GetAllAsync();
                var user = allUsers.FirstOrDefault(u =>
                    u.UserName.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email?.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) == true));

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                return ServiceResult<UserDto>.Success(user);
            }, nameof(GetByUsernameOrEmailAsync));
        }

        public async Task<ServiceResult<bool>> UpdateLastLoginTimeAsync(Guid userId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.LastLoginTime = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(UpdateLastLoginTimeAsync));
        }

        public async Task<ServiceResult<bool>> IncrementFailedLoginCountAsync(Guid userId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.FailedLoginCount++;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(IncrementFailedLoginCountAsync));
        }

        public async Task<ServiceResult<bool>> ResetFailedLoginCountAsync(Guid userId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.FailedLoginCount = 0;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(ResetFailedLoginCountAsync));
        }

        public async Task<ServiceResult<bool>> IsAccountLockedAsync(Guid userId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 简化逻辑：失败次数>=5或状态为禁用即为锁定
                var isLocked = user.FailedLoginCount >= 5 || user.Status == CommonStatus.Disabled;
                return ServiceResult<bool>.Success(isLocked);
            }, nameof(IsAccountLockedAsync));
        }

        #endregion

        #region 业务操作

        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建用户: {dto.Username}");

                // 转换DTO - 注意CreateDto使用Username而不是UserName
                var user = new UserDto
                {
                    Id = Guid.NewGuid(),
                    UserName = dto.Username,
                    RealName = dto.RealName,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    Role = dto.Role,
                    Status = dto.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // TODO: 密码处理应该在Repository或更底层处理
                // 当前简化实现，实际应该加密dto.Password

                var created = await _repository.CreateAsync(user);
                return ServiceResult<UserDto>.Success(created);
            }, nameof(CreateUserAsync));
        }

        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 更新字段 - UpdateDto的字段是可选的
                if (!string.IsNullOrEmpty(dto.RealName))
                {
                    existing.RealName = dto.RealName;
                }

                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    existing.PhoneNumber = dto.PhoneNumber;
                }

                if (!string.IsNullOrEmpty(dto.Email))
                {
                    existing.Email = dto.Email;
                }

                if (dto.Role.HasValue)
                {
                    existing.Role = dto.Role.Value;
                }

                existing.Status = dto.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<UserDto>.Success(updated);
            }, nameof(UpdateUserAsync));
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult<bool>.Success(true);
            }, nameof(DeleteUserAsync));
        }

        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.Status = CommonStatus.Disabled;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(DisableAsync));
        }

        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.Status = CommonStatus.Enabled;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(EnableAsync));
        }

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var result = await DisableAsync(id);
                    if (result.IsSuccess)
                        count++;
                }
                return ServiceResult<int>.Success(count);
            }, nameof(BatchDisableAsync));
        }

        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var result = await EnableAsync(id);
                    if (result.IsSuccess)
                        count++;
                }
                return ServiceResult<int>.Success(count);
            }, nameof(BatchEnableAsync));
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            // Desktop Client不实现密码重置逻辑，应该由Server端处理
            return await Task.FromResult(ServiceResult<bool>.Failure("Desktop Client不支持密码重置"));
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            // Desktop Client不实现密码修改逻辑，应该由Server端处理
            return await Task.FromResult(ServiceResult<bool>.Failure("Desktop Client不支持密码修改"));
        }

        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                user.RealName = realName;
                user.PhoneNumber = phoneNumber;
                user.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(user);

                return ServiceResult<bool>.Success(true);
            }, nameof(ChangeProfileAsync));
        }

        #endregion
    }
}
