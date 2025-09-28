using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户服务实现 - 包含完整的CRUD和认证功能
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        #region 查询操作

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(entity);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败");
                return ServiceResult<UserDto>.Failure("获取用户详情失败");
            }
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(query.PageIndex, query.PageSize);
                var dto = new PagedResult<UserDto>
                {
                    Items = _mapper.Map<List<UserDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<UserDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
            }
        }

        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName)
        {
            try
            {
                var entity = await _repository.GetByUsernameAsync(userName);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(entity);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败: {UserName}", userName);
                return ServiceResult<UserDto>.Failure("获取用户失败");
            }
        }

        public async Task<ServiceResult<UserDto>> GetByEmailAsync(string email)
        {
            try
            {
                var entity = await _repository.GetByEmailAsync(email);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(entity);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据邮箱获取用户失败: {Email}", email);
                return ServiceResult<UserDto>.Failure("获取用户失败");
            }
        }

        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            try
            {
                var entities = await _repository.FindAsync(u => u.Status == CommonStatus.Enabled);
                var dto = _mapper.Map<List<UserDto>>(entities);
                return ServiceResult<List<UserDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取启用用户列表失败");
                return ServiceResult<List<UserDto>>.Failure("获取启用用户列表失败");
            }
        }

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(u => 
                    u.UsernName.Contains(keyword) || 
                    u.RealName.Contains(keyword) || 
                    (u.Email != null && u.Email.Contains(keyword)));
                var dto = _mapper.Map<List<UserDto>>(entities);
                return ServiceResult<List<UserDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure("搜索用户失败");
            }
        }

        public Task<ServiceResult<List<object>>> GetRolesAsync()
        {
            try
            {
                // 返回用户角色枚举
                var roles = Enum.GetValues<UserRole>()
                    .Select(r => new { Value = (int)r, Name = r.ToString() })
                    .Cast<object>()
                    .ToList();

                return Task.FromResult(ServiceResult<List<object>>.Success(roles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色列表失败");
                return Task.FromResult(ServiceResult<List<object>>.Failure("获取角色列表失败"));
            }
        }

        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string userName)
        {
            try
            {
                var exists = await _repository.ExistsAsync(u => u.UsernName == userName);
                return ServiceResult<bool>.Success(!exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户名失败: {UserName}", userName);
                return ServiceResult<bool>.Failure("验证用户名失败");
            }
        }

        public async Task<ServiceResult<List<UserDto>>> GetDoctorsAsync()
        {
            try
            {
                var entities = await _repository.FindAsync(u => u.Role == UserRole.Doctor);
                var dto = _mapper.Map<List<UserDto>>(entities);
                return ServiceResult<List<UserDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生列表失败");
                return ServiceResult<List<UserDto>>.Failure("获取医生列表失败");
            }
        }

        public async Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(doctorId);
                bool isAvailable = entity != null && 
                                  entity.Role == UserRole.Doctor && 
                                  entity.Status == CommonStatus.Enabled &&
                                  !entity.IsDeleted;
                
                return ServiceResult<bool>.Success(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查医生可用性失败: {DoctorId}", doctorId);
                return ServiceResult<bool>.Failure("检查医生可用性失败");
            }
        }

        #endregion

        #region 认证操作

        public async Task<ServiceResult<bool>> ValidatePasswordAsync(Guid userId, string password)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 使用BCrypt验证密码
                bool isValid = BCrypt.Net.BCrypt.Verify(password, entity.PasswordHash);
                return ServiceResult<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码失败: {UserId}", userId);
                return ServiceResult<bool>.Failure("验证密码失败");
            }
        }

        public async Task<ServiceResult<UserDto>> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            try
            {
                // 先尝试按用户名查找
                var entity = await _repository.GetByUsernameAsync(usernameOrEmail);
                
                // 如果未找到，再尝试按邮箱查找
                if (entity == null)
                {
                    entity = await _repository.GetByEmailAsync(usernameOrEmail);
                }

                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDto>(entity);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名或邮箱获取用户失败: {UsernameOrEmail}", usernameOrEmail);
                return ServiceResult<UserDto>.Failure("获取用户失败");
            }
        }

        public async Task<ServiceResult<bool>> UpdateLastLoginTimeAsync(Guid userId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.LastLoginTime = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新最后登录时间失败: {UserId}", userId);
                return ServiceResult<bool>.Failure("更新最后登录时间失败");
            }
        }

        public async Task<ServiceResult<bool>> IncrementFailedLoginCountAsync(Guid userId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.FailedLoginCount++;
                
                // 如果失败次数达到5次，锁定账户1小时
                if (entity.FailedLoginCount >= 5)
                {
                    entity.LockoutEnd = DateTime.UtcNow.AddHours(1);
                }
                
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "增加失败登录次数失败: {UserId}", userId);
                return ServiceResult<bool>.Failure("增加失败登录次数失败");
            }
        }

        public async Task<ServiceResult<bool>> ResetFailedLoginCountAsync(Guid userId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.FailedLoginCount = 0;
                entity.LockoutEnd = null;
                await _repository.UpdateAsync(entity);
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置失败登录次数失败: {UserId}", userId);
                return ServiceResult<bool>.Failure("重置失败登录次数失败");
            }
        }

        public async Task<ServiceResult<bool>> IsAccountLockedAsync(Guid userId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                bool isLocked = entity.LockoutEnd.HasValue && entity.LockoutEnd.Value > DateTime.UtcNow;
                return ServiceResult<bool>.Success(isLocked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查账户锁定状态失败: {UserId}", userId);
                return ServiceResult<bool>.Failure("检查账户锁定状态失败");
            }
        }

        #endregion

        #region 业务操作

        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = _mapper.Map<User>(dto);
                
                // 对密码进行哈希处理
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }
                
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return ServiceResult<UserDto>.Failure("创建用户失败");
            }
        }

        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return ServiceResult<UserDto>.Failure("更新用户失败");
            }
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return ServiceResult<bool>.Failure("删除用户失败");
            }
        }

        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.Status = CommonStatus.Disabled;
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败");
                return ServiceResult<bool>.Failure("禁用用户失败");
            }
        }

        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.Status = CommonStatus.Enabled;
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败");
                return ServiceResult<bool>.Failure("启用用户失败");
            }
        }

        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var result = await DisableAsync(id);
                    if (result.IsSuccess) count++;
                }
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败");
                return ServiceResult<int>.Failure("批量禁用用户失败");
            }
        }

        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                int count = 0;
                foreach (var id in ids)
                {
                    var result = await EnableAsync(id);
                    if (result.IsSuccess) count++;
                }
                return ServiceResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败");
                return ServiceResult<int>.Failure("批量启用用户失败");
            }
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败");
                return ServiceResult<bool>.Failure("重置密码失败");
            }
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 验证旧密码
                if (!BCrypt.Net.BCrypt.Verify(oldPassword, entity.PasswordHash))
                    return ServiceResult<bool>.Failure("原密码错误");

                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更改密码失败");
                return ServiceResult<bool>.Failure("更改密码失败");
            }
        }

        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                entity.RealName = realName;
                entity.PhoneNumber = phoneNumber;
                await _repository.UpdateAsync(entity);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人信息失败");
                return ServiceResult<bool>.Failure("修改个人信息失败");
            }
        }

        #endregion
    }
}