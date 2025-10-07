using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户服务实现 - 标准CRUD模式
    /// Issue #1008: 重构为标准Service，移除过度设计方法
    /// 遵循单一服务原则，符合MVP适度设计原则
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserService> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        #region 查询操作

        /// <summary>
        /// 分页获取用户列表（统一参数版本）
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dtos = _mapper.Map<List<UserDto>>(pagedResult.Items);

                // 如果有关键字，进行内存过滤（MVP阶段简化处理）
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    dtos = dtos.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (u.Email != null && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var result = new PagedResult<UserDto>
                {
                    Items = dtos,
                    TotalCount = keyword == null ? pagedResult.TotalCount : dtos.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<UserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
            }
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
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

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(u =>
                    u.UserName.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.Email != null && u.Email.Contains(keyword)));

                var dtos = _mapper.Map<List<UserDto>>(entities);
                return ServiceResult<List<UserDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
                return ServiceResult<List<UserDto>>.Failure("搜索用户失败");
            }
        }

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 获取超级管理员用户名（可配置）
                var sysAdminUsername = _configuration["Lybt:Business:SystemAdmin:Username"] ?? "clinic_admin";

                // 检查是否尝试使用超级管理员用户名
                if (string.Equals(dto.Username, sysAdminUsername, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("尝试创建与超级管理员相同的用户名: {Username}", dto.Username);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.Username}' 为系统保留用户名，不可使用");
                }

                // 可选：添加其他保留用户名列表
                var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
                if (reservedUsernames.Any(reserved => string.Equals(dto.Username, reserved, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("尝试创建保留用户名: {Username}", dto.Username);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.Username}' 为系统保留用户名，不可使用");
                }

                var entity = _mapper.Map<User>(dto);

                // 对密码进行哈希处理
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                }

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);

                _logger.LogInformation("成功创建用户: {Username}, Role: {Role}", resultDto.UserName, resultDto.Role);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return ServiceResult<UserDto>.Failure("创建用户失败");
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // 注意：UserUpdateDto不包含Username属性，用户名一旦创建不可更改
                // 这也避免了用户后期尝试改为超级管理员用户名的风险

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);

                _logger.LogInformation("成功更新用户: {UserId}", id);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return ServiceResult<UserDto>.Failure("更新用户失败");
            }
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return ServiceResult.Failure("删除用户失败");
            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("用户不存在");

                entity.Status = CommonStatus.Disabled;
                await _repository.UpdateAsync(entity);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败");
                return ServiceResult.Failure("禁用用户失败");
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("用户不存在");

                entity.Status = CommonStatus.Enabled;
                await _repository.UpdateAsync(entity);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败");
                return ServiceResult.Failure("启用用户失败");
            }
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        public async Task<ServiceResult> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("用户不存在");

                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _repository.UpdateAsync(entity);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败");
                return ServiceResult.Failure("重置密码失败");
            }
        }

        /// <summary>
        /// 更改密码
        /// </summary>
        public async Task<ServiceResult> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("用户不存在");

                // 验证旧密码
                if (!BCrypt.Net.BCrypt.Verify(oldPassword, entity.PasswordHash))
                    return ServiceResult.Failure("原密码错误");

                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _repository.UpdateAsync(entity);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更改密码失败");
                return ServiceResult.Failure("更改密码失败");
            }
        }

        /// <summary>
        /// 修改个人信息
        /// </summary>
        public async Task<ServiceResult> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                    return ServiceResult.Failure("用户不存在");

                entity.RealName = realName;
                entity.PhoneNumber = phoneNumber;
                await _repository.UpdateAsync(entity);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人信息失败");
                return ServiceResult.Failure("修改个人信息失败");
            }
        }

        #endregion
    }
}
