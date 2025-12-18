using AutoMapper;
using FluentValidation;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using LYBT.Shared.Utilities.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户服务实现 - 标准CRUD模式
    /// Issue #1008: 重构为标准Service，移除过度设计方法
    /// Issue #1909: 添加三角色权限控制（SuperAdmin/Admin/Doctor）
    /// Phase 2: 继承BaseService<User>复用统一错误处理和验证逻辑
    /// 遵循单一服务原则，符合MVP适度设计原则
    /// </summary>
    public class UserService : BaseService<User>, IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<UserInputDto> _validator;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IValidator<UserInputDto> validator)
            : base(logger, mapper)
        {
            _repository = repository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        #region 权限检查辅助方法（Issue #1909）

        /// <summary>
        /// 获取当前用户角色
        /// </summary>
        private UserRole? GetCurrentUserRole()
        {
            try
            {
                var roleClaim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(roleClaim))
                    return null;

                return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查当前用户是否可以管理目标用户
        /// 权限规则：
        /// - SuperAdmin（100）可以管理 Admin 和 Doctor
        /// - Admin（10）可以管理 Doctor，但不能管理 Admin 或 SuperAdmin
        /// - Doctor（1）只能修改自己的信息
        /// </summary>
        private bool CanManageUser(UserRole? currentUserRole, UserRole? targetUserRole)
        {
            if (!currentUserRole.HasValue || !targetUserRole.HasValue)
                return false;

            return currentUserRole.Value switch
            {
                UserRole.SuperAdmin => true,  // SuperAdmin可以管理所有用户
                UserRole.Admin => targetUserRole.Value == UserRole.Doctor,  // Admin只能管理Doctor
                UserRole.Doctor => false,  // Doctor不能管理其他用户
                _ => false
            };
        }

        /// <summary>
        /// 检查是否可以删除指定用户（包含最后一个保护）
        /// </summary>
        private async Task<Result> CanDeleteUserAsync(Guid userId, UserRole targetRole)
        {
            // 权限检查
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, targetRole))
            {
                return Result.Failure("您没有权限删除该用户");
            }

            // 最后一个SuperAdmin/Admin保护
            if (targetRole == UserRole.SuperAdmin || targetRole == UserRole.Admin)
            {
                // 使用FindAsync查询符合条件的用户数量（IRepository<T>无带参数的CountAsync）
                var users = await _repository.FindAsync(u => u.Role == targetRole);
                var count = users.Count();
                if (count <= 1)
                {
                    var roleName = targetRole == UserRole.SuperAdmin ? "超级管理员" : "管理员";
                    return Result.Failure($"不能删除最后一个{roleName}");
                }
            }

            return Result.Success();
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 分页获取用户列表（返回UserListDto，用于列表视图）
        /// OpenSpec: refactor-dto-simplification - 使用扁平化DTO
        /// </summary>
        public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dtos = _mapper.Map<List<UserListDto>>(pagedResult.Items);

                // 应用筛选条件（MVP阶段内存过滤）
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    dtos = dtos.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (role.HasValue)
                {
                    dtos = dtos.Where(u => u.Role == role.Value).ToList();
                }

                if (status.HasValue)
                {
                    dtos = dtos.Where(u => u.Status == status.Value).ToList();
                }

                var result = new PagedResult<UserListDto>
                {
                    Items = dtos,
                    TotalCount = keyword == null && !role.HasValue && !status.HasValue
                        ? pagedResult.TotalCount
                        : dtos.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return Result<PagedResult<UserListDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return Result<PagedResult<UserListDto>>.Failure("获取用户列表失败");
            }
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<UserDetailDto>.Failure("用户不存在");

                var dto = _mapper.Map<UserDetailDto>(entity);
                return Result<UserDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详情失败");
                return Result<UserDetailDto>.Failure("获取用户详情失败");
            }
        }

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        public async Task<Result<List<UserListDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(u =>
                    u.UserName.Contains(keyword) ||
                    u.RealName.Contains(keyword) ||
                    (u.Email != null && u.Email.Contains(keyword)));

                var dtos = _mapper.Map<List<UserListDto>>(entities);
                return Result<List<UserListDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
                return Result<List<UserListDto>>.Failure("搜索用户失败");
            }
        }

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<Result<UserDetailDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // FluentValidation 验证（Phase 1 Task 1.6）
                var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("用户创建验证失败: {Errors}", string.Join("; ", errors));
                    return Result<UserDetailDto>.Failure(errors);
                }

                // Issue #1909: 权限检查 - 不能创建比自己权限高的角色
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, dto.Role))
                {
                    var roleName = dto.Role == UserRole.SuperAdmin ? "超级管理员" :
                                   dto.Role == UserRole.Admin ? "管理员" : "医生";
                    _logger.LogWarning("用户 {CurrentRole} 尝试创建更高权限的用户: {TargetRole}",
                        currentRole, dto.Role);
                    return Result<UserDetailDto>.Failure($"您没有权限创建{roleName}账户");
                }

                // 保留用户名检查（Issue #1909: 简化，仅保留系统保留用户名）
                var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
                if (reservedUsernames.Any(reserved => string.Equals(dto.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("尝试创建保留用户名: {UserName}", dto.UserName);
                    return Result<UserDetailDto>.Failure($"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
                }

                // Issue #1262: 检查用户名是否已存在（唯一性验证）
                // 使用IUserRepository特定方法检查用户名是否存在
                // FluentValidation已确保UserName不为null，使用!操作符消除编译器警告
                var existingUser = await _repository.UsernameExistsAsync(dto.UserName!);
                if (existingUser)
                {
                    _logger.LogWarning("尝试创建重复的用户名: {UserName}", dto.UserName);
                    return Result<UserDetailDto>.Failure($"用户名 '{dto.UserName}' 已存在，请使用其他用户名");
                }

                var entity = _mapper.Map<User>(dto);

                // Issue #1911: 生成拼音码（基于RealName）
                entity.PinYinCode = PinYinHelper.GetPinYinCode(dto.RealName);

                // Issue #1262: 对密码进行哈希处理，如果未提供密码则使用默认密码
                string passwordToHash;
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    passwordToHash = dto.Password;
                    _logger.LogDebug("使用用户提供的密码创建用户: {UserName}", dto.UserName);
                }
                else
                {
                    // 从配置读取默认密码：Lybt:DefaultPasswords:NewUserPassword
                    passwordToHash = _configuration["Lybt:DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";
                    _logger.LogInformation("使用系统默认密码创建用户: {UserName}，密码配置: Lybt:DefaultPasswords:NewUserPassword", dto.UserName);
                }

                // Issue #2547: 使用统一PasswordHelper进行密码哈希
                entity.PasswordHash = PasswordHelper.HashPassword(passwordToHash, entity.Role, _logger);

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<UserDetailDto>(result);

                _logger.LogInformation("成功创建用户: {UserName}, Role: {Role}", resultDto.UserName, resultDto.Role);
                return Result<UserDetailDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败");
                return Result<UserDetailDto>.Failure("创建用户失败");
            }
        }

        /// <summary>
        /// 更新用户
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<UserDetailDto>.Failure("用户不存在");

                // FluentValidation 验证（Phase 1 Task 1.6）
                var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("用户更新验证失败: {UserId}, {Errors}", id, string.Join("; ", errors));
                    return Result<UserDetailDto>.Failure(errors);
                }

                // Issue #1909: 权限检查 - 只能更新有权限管理的用户
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, entity.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试更新无权限的用户: {TargetUserId}, {TargetRole}",
                        currentRole, id, entity.Role);
                    return Result<UserDetailDto>.Failure("您没有权限更新该用户");
                }

                // Issue #1909: 检查角色变更权限
                if (dto.Role != entity.Role && !CanManageUser(currentRole, dto.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试将用户角色改为更高权限: {OldRole} -> {NewRole}",
                        currentRole, entity.Role, dto.Role);
                    return Result<UserDetailDto>.Failure("您没有权限将用户角色修改为该级别");
                }

                // 注意：UserInputDto不包含Username属性，用户名一旦创建不可更改
                // 这也避免了用户后期尝试改为超级管理员用户名的风险

                // Issue #1911: 保存原 RealName 用于比较
                var oldRealName = entity.RealName;
                _mapper.Map(dto, entity);

                // Issue #1911: 更新拼音码（仅当RealName发生变化时）
                if (!string.IsNullOrWhiteSpace(dto.RealName) && dto.RealName != oldRealName)
                {
                    entity.PinYinCode = PinYinHelper.GetPinYinCode(dto.RealName);
                    _logger.LogDebug("RealName变化，重新生成拼音码: {OldName} -> {NewName}, PinYin: {PinYin}",
                        oldRealName, dto.RealName, entity.PinYinCode);
                }

                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDetailDto>(result);

                _logger.LogInformation("成功更新用户: {UserId}", id);
                return Result<UserDetailDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败");
                return Result<UserDetailDto>.Failure("更新用户失败");
            }
        }

        /// <summary>
        /// 删除用户（软删除）
        /// Issue #1909: 添加三角色权限控制和最后一个保护
        /// </summary>
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                // 获取目标用户
                var targetUser = await _repository.GetByIdAsync(id);
                if (targetUser == null)
                    return Result.Failure("用户不存在");

                // Issue #1909: 权限检查和保护逻辑
                var permissionCheck = await CanDeleteUserAsync(id, targetUser.Role);
                if (!permissionCheck.IsSuccess)
                {
                    _logger.LogWarning("删除用户权限检查失败: {UserId}, {Reason}",
                        id, permissionCheck.Message);
                    return permissionCheck;
                }

                var result = await _repository.DeleteAsync(id);
                _logger.LogInformation("成功删除用户: {UserId}, Role: {Role}", id, targetUser.Role);
                return result ? Result.Success() : Result.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return Result.Failure("删除用户失败");
            }
        }

        /// <summary>
        /// 管理员重置密码（Issue #1162: 使用配置文件中的默认密码）
        /// 修复：重置密码不再接受新密码参数，始终使用配置文件中的默认密码
        /// </summary>
        public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<ResetPasswordResponseDto>.Failure("用户不存在");

                // 始终使用配置文件中的默认密码，不再接受请求中的密码参数
                string password = _configuration["Lybt:DefaultPasswords:NewUserPassword"]
                    ?? PasswordHelper.GenerateTemporaryPassword();

                // 哈希密码并更新
                entity.PasswordHash = PasswordHelper.HashPassword(password, entity.Role, _logger);
                await _repository.UpdateAsync(entity);

                var response = new ResetPasswordResponseDto
                {
                    Success = true,
                    TemporaryPassword = password
                };

                _logger.LogInformation("重置用户密码成功: {UserId}, 使用配置默认密码",
                    id);

                return Result<ResetPasswordResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败");
                return Result<ResetPasswordResponseDto>.Failure("重置密码失败");
            }
        }

        /// <summary>
        /// 验证用户密码
        /// Issue #1864: Auth/User职责分离，密码验证由UserService负责
        /// </summary>
        public async Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                    return Result<UserDetailDto>.Failure("用户名不能为空");

                if (string.IsNullOrWhiteSpace(password))
                    return Result<UserDetailDto>.Failure("密码不能为空");

                var entity = await _repository.GetByUsernameAsync(userName);
                if (entity == null)
                {
                    _logger.LogWarning("用户不存在: {UserName}", userName);
                    return Result<UserDetailDto>.Failure("用户名或密码错误");
                }

                // 检查用户状态
                if (entity.Status == CommonStatus.Disabled)
                {
                    _logger.LogWarning("用户已被禁用: {UserName}", userName);
                    return Result<UserDetailDto>.Failure("用户已被禁用");
                }

                // 验证密码
                var verificationResult = PasswordHelper.VerifyPassword(password, entity.PasswordHash, entity.Role, _logger);
                if (!verificationResult.IsSuccess)
                {
                    _logger.LogWarning("密码验证失败: {UserName}", userName);
                    return Result<UserDetailDto>.Failure("用户名或密码错误");
                }

                // 如果需要重新哈希密码（升级哈希算法场景）
                if (verificationResult.NewHashedPassword != null)
                {
                    entity.PasswordHash = verificationResult.NewHashedPassword;
                    await _repository.UpdateAsync(entity);
                    _logger.LogInformation("用户密码哈希已升级: {UserName}", userName);
                }

                var userDto = _mapper.Map<UserDetailDto>(entity);
                return Result<UserDetailDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户密码时发生异常: {UserName}", userName);
                return Result<UserDetailDto>.Failure("验证密码时发生内部错误");
            }
        }

        /// <summary>
        /// 更改密码
        /// </summary>
        public async Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result.Failure("用户不存在");

                // 验证旧密码并获取验证结果
                var verificationResult = PasswordHelper.VerifyPassword(oldPassword, entity.PasswordHash, entity.Role, _logger);
                if (!verificationResult.IsSuccess)
                    return Result.Failure("原密码错误");

                // 如果需要重新哈希，使用新的哈希
                var newHashedPassword = verificationResult.NewHashedPassword ?? PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
                entity.PasswordHash = newHashedPassword;
                await _repository.UpdateAsync(entity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更改密码失败");
                return Result.Failure("更改密码失败");
            }
        }

        /// <summary>
        /// 修改个人信息 (Issue #1888)
        /// </summary>
        public async Task<Result<UserDetailDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
        {
            try
            {
                // 验证输入
                if (dto == null)
                {
                    return Result<UserDetailDto>.Failure("个人资料信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(dto.RealName))
                {
                    return Result<UserDetailDto>.Failure("真实姓名不能为空");
                }

                // 获取用户实体
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                {
                    _logger.LogWarning("尝试修改不存在的用户资料: {UserId}", userId);
                    return Result<UserDetailDto>.Failure("用户不存在");
                }

                // 更新字段
                entity.RealName = dto.RealName;
                entity.PhoneNumber = dto.PhoneNumber;

                // 保存更改
                var updatedEntity = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDetailDto>(updatedEntity);

                _logger.LogInformation("成功修改用户资料: {UserId}, RealName: {RealName}", userId, dto.RealName);
                return Result<UserDetailDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人资料失败: {UserId}", userId);
                return Result<UserDetailDto>.Failure("修改个人资料失败");
            }
        }

        #endregion

        #region 私有辅助方法

        // Issue #1757: GenerateTemporaryPassword已移至PasswordHelper.GenerateTemporaryPassword

        #endregion

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        public async Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Result<UserDetailDto>.Failure("用户不存在");

                // 权限检查
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, entity.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试切换无权限的用户状态: {TargetUserId}, {TargetRole}",
                        currentRole, id, entity.Role);
                    return Result<UserDetailDto>.Failure("您没有权限修改该用户状态");
                }

                entity.Status = entity.Status == CommonStatus.Enabled
                    ? CommonStatus.Disabled
                    : CommonStatus.Enabled;
                entity.UpdatedAt = DateTime.Now;

                var result = await _repository.UpdateAsync(entity);
                var dto = _mapper.Map<UserDetailDto>(result);

                _logger.LogInformation("用户状态已切换: {UserId}, 新状态: {Status}", id, entity.Status);
                return Result<UserDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态失败: {UserId}", id);
                return Result<UserDetailDto>.Failure("切换用户状态失败");
            }
        }

        /// <summary>
        /// 恢复软删除的用户
        /// </summary>
        public async Task<Result<UserDetailDto>> RestoreAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdIncludingDeletedAsync(id);
                if (entity == null)
                    return Result<UserDetailDto>.Failure("用户不存在");

                if (!entity.IsDeleted)
                    return Result<UserDetailDto>.Failure("该用户未被删除，无需恢复");

                // 权限检查
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, entity.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试恢复无权限的用户: {TargetUserId}, {TargetRole}",
                        currentRole, id, entity.Role);
                    return Result<UserDetailDto>.Failure("您没有权限恢复该用户");
                }

                entity.IsDeleted = false;
                entity.UpdatedAt = DateTime.Now;

                var result = await _repository.UpdateAsync(entity);
                var dto = _mapper.Map<UserDetailDto>(result);

                _logger.LogInformation("用户已恢复: {UserId}, {UserName}", id, entity.UserName);
                return Result<UserDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复用户失败: {UserId}", id);
                return Result<UserDetailDto>.Failure("恢复用户失败");
            }
        }
    }
}
