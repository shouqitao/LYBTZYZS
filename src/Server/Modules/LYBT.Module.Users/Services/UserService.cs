using System.Security.Claims;
using LYBT.Module.Users.Mapping;
using FluentValidation;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using LYBT.Shared.Utilities.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

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
        private readonly ICrossModuleAuthService _authService;
        private readonly UserMapper _mapper = new();

        public UserService(
            IUserRepository repository,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IValidator<UserInputDto> validator,
            ICrossModuleAuthService authService)
            : base(logger)
        {
            _repository = repository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
            _authService = authService;
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
        /// S2: 获取当前用户ID
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var id) ? id : null;
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
                UserRole.Admin => targetUserRole.Value is UserRole.Doctor or UserRole.Receptionist,  // S2: Admin管理Doctor+Receptionist
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
                return Result.Failure(GenericErrorCode.Forbidden, "您没有权限删除该用户");
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
                    return Result.Failure(GenericErrorCode.CannotDeleteSysAdmin, $"不能删除最后一个{roleName}");
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
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var pagedResult = await _repository.GetPagedAsync(page, pageSize);
            var dtos = _mapper.ToListDtos(pagedResult.Items.ToList());

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

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            var dto = _mapper.ToDetailDto(entity);
            return Result<UserDetailDto>.Success(dto);
        }

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        public async Task<Result<List<UserListDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entities = await _repository.FindAsync(u =>
                u.UserName.Contains(keyword) ||
                u.RealName.Contains(keyword) ||
                (u.Email != null && u.Email.Contains(keyword)));

            var dtos = _mapper.ToListDtos(entities.ToList());
            return Result<List<UserListDto>>.Success(dtos);
        }

        #endregion

        #region 业务操作

        /// <summary>
        /// 创建用户
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<Result<UserDetailDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理

            // FluentValidation 验证（Phase 1 Task 1.6）
            var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] User.Create → ValidationFailed - Errors={Errors}", string.Join("; ", errors));
                return Result<UserDetailDto>.Failure(errors);
            }

            // Issue #1909: 权限检查 - 不能创建比自己权限高的角色
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, dto.Role))
            {
                var roleName = dto.Role == UserRole.SuperAdmin ? "超级管理员" :
                               dto.Role == UserRole.Admin ? "管理员" : "医生";
                _logger.LogWarning("[SVC] User.Create → PermissionDenied - CurrentRole={CurrentRole} TargetRole={TargetRole}",
                    currentRole, dto.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, $"您没有权限创建{roleName}账户");
            }

            // 保留用户名检查（Issue #1909: 简化，仅保留系统保留用户名）
            var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
            if (reservedUsernames.Any(reserved => string.Equals(dto.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("[SVC] User.Create → ReservedUsername - UserName={UserName}", dto.UserName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNameExists, $"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
            }

            // Issue #1262: 检查用户名是否已存在（唯一性验证）
            // 使用IUserRepository特定方法检查用户名是否存在
            // FluentValidation已确保UserName不为null，使用!操作符消除编译器警告
            var existingUser = await _repository.UsernameExistsAsync(dto.UserName!);
            if (existingUser)
            {
                _logger.LogWarning("[SVC] User.Create → DuplicateUsername - UserName={UserName}", dto.UserName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNameExists, $"用户名 '{dto.UserName}' 已存在，请使用其他用户名");
            }

            var entity = _mapper.ToEntity(dto);

            // Issue #1911: 生成拼音码（基于RealName）
            entity.PinYinCode = PinYinHelper.GetPinYinCode(dto.RealName);

            // Issue #1262: 对密码进行哈希处理，如果未提供密码则使用默认密码
            string passwordToHash;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                passwordToHash = dto.Password;
                _logger.LogDebug("[SVC] User.Create → UserPassword - UserName={UserName}", dto.UserName);
            }
            else
            {
                // 从配置读取默认密码：Lybt:DefaultPasswords:NewUserPassword
                passwordToHash = _configuration["Lybt:DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";
                _logger.LogInformation("[SVC] User.Create → DefaultPassword - UserName={UserName}", dto.UserName);
            }

            // Issue #2547: 使用统一PasswordHelper进行密码哈希
            entity.PasswordHash = PasswordHelper.HashPassword(passwordToHash, entity.Role, _logger);

            var result = await _repository.AddAsync(entity);
            var resultDto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] User.Create completed - UserName={UserName} Role={Role}", resultDto.UserName, resultDto.Role);
            return Result<UserDetailDto>.Success(resultDto);
        }

        /// <summary>
        /// 更新用户
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            // FluentValidation 验证（Phase 1 Task 1.6）
            var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] User.Update → ValidationFailed - UserId={UserId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<UserDetailDto>.Failure(errors);
            }

            // Issue #1909: 权限检查 - 只能更新有权限管理的用户
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, entity.Role))
            {
                _logger.LogWarning("[SVC] User.Update → PermissionDenied - CurrentRole={CurrentRole} UserId={UserId} TargetRole={TargetRole}",
                    currentRole, id, entity.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限更新该用户");
            }

            // Issue #1909: 检查角色变更权限
            if (dto.Role != entity.Role && !CanManageUser(currentRole, dto.Role))
            {
                _logger.LogWarning("[SVC] User.Update → RoleEscalation - CurrentRole={CurrentRole} OldRole={OldRole} NewRole={NewRole}",
                    currentRole, entity.Role, dto.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限将用户角色修改为该级别");
            }

            // 注意：UserInputDto不包含Username属性，用户名一旦创建不可更改
            // 这也避免了用户后期尝试改为超级管理员用户名的风险

            // X3-02: 角色变更时撤销所有 Token，强制重新认证获取新权限
            var roleChanged = dto.Role != entity.Role;

            // Issue #1911: 保存原 RealName 用于比较
            var oldRealName = entity.RealName;
            _mapper.UpdateEntity(dto, entity);

            // Issue #1911: 更新拼音码（仅当RealName发生变化时）
            if (!string.IsNullOrWhiteSpace(dto.RealName) && dto.RealName != oldRealName)
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(dto.RealName);
                _logger.LogDebug("[SVC] User.Update → PinYinRegenerated - OldName={OldName} NewName={NewName} PinYin={PinYin}",
                    oldRealName, dto.RealName, entity.PinYinCode);
            }

            var result = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.ToDetailDto(result);

            // X3-02: 角色变更后撤销 Token
            if (roleChanged)
            {
                await _authService.RevokeUserTokensAsync(id, "角色变更，强制重新认证");
            }

            _logger.LogInformation("[SVC] User.Update completed - UserId={UserId}", id);
            return Result<UserDetailDto>.Success(resultDto);
        }

        /// <summary>
        /// 删除用户（软删除）
        /// Issue #1909: 添加三角色权限控制和最后一个保护
        /// </summary>
        public async Task<Result> DeleteAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理

            // S2: 自删除保护
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                _logger.LogWarning("[SVC] User.Delete → SelfDeleteBlocked - UserId={UserId}", id);
                return Result.Failure(GenericErrorCode.Forbidden, "不能删除自己的账户");
            }

            // 获取目标用户
            var targetUser = await _repository.GetByIdAsync(id);
            if (targetUser == null)
                return Result.Failure(GenericErrorCode.UserNotFound);

            // Issue #1909: 权限检查和保护逻辑
            var permissionCheck = await CanDeleteUserAsync(id, targetUser.Role);
            if (!permissionCheck.IsSuccess)
            {
                _logger.LogWarning("[SVC] User.Delete → PermissionDenied - UserId={UserId} Reason={Reason}",
                    id, permissionCheck.Message);
                return permissionCheck;
            }

            // X3-03: 删除前撤销所有 Token
            await _authService.RevokeUserTokensAsync(id, "用户已删除");

            var result = await _repository.DeleteAsync(id);
            _logger.LogInformation("[SVC] User.Delete completed - UserId={UserId} Role={Role}", id, targetUser.Role);
            return result ? Result.Success() : Result.Failure(GenericErrorCode.InternalError, "删除失败");
        }

        /// <summary>
        /// 管理员重置密码（Issue #1162: 使用配置文件中的默认密码）
        /// 修复：重置密码不再接受新密码参数，始终使用配置文件中的默认密码
        /// </summary>
        public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<ResetPasswordResponseDto>.Failure(GenericErrorCode.UserNotFound);

            // 始终使用配置文件中的默认密码，不再接受请求中的密码参数
            string password = _configuration["Lybt:DefaultPasswords:NewUserPassword"]
                ?? PasswordHelper.GenerateTemporaryPassword();

            // 哈希密码并更新
            entity.PasswordHash = PasswordHelper.HashPassword(password, entity.Role, _logger);
            await _repository.UpdateAsync(entity);

            // X3-04: 重置密码后撤销所有 Token，强制重新登录
            await _authService.RevokeUserTokensAsync(id, "密码已重置，强制重新登录");

            var response = new ResetPasswordResponseDto
            {
                Success = true,
                TemporaryPassword = password
            };

            _logger.LogInformation("[SVC] User.ResetPassword completed - UserId={UserId}", id);

            return Result<ResetPasswordResponseDto>.Success(response);
        }

        /// <summary>
        /// 验证用户密码
        /// Issue #1864: Auth/User职责分离，密码验证由UserService负责
        /// </summary>
        public async Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            if (string.IsNullOrWhiteSpace(userName))
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "用户名不能为空");

            if (string.IsNullOrWhiteSpace(password))
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "密码不能为空");

            var entity = await _repository.GetByUsernameAsync(userName);
            if (entity == null)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → NotFound - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            // 检查用户状态
            if (entity.Status == CommonStatus.Disabled)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → Disabled - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
            }

            // 验证密码
            var verificationResult = PasswordHelper.VerifyPassword(password, entity.PasswordHash, entity.Role, _logger);
            if (!verificationResult.IsSuccess)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → InvalidPassword - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            // 如果需要重新哈希密码（升级哈希算法场景）
            if (verificationResult.NewHashedPassword != null)
            {
                entity.PasswordHash = verificationResult.NewHashedPassword;
                await _repository.UpdateAsync(entity);
                _logger.LogInformation("[SVC] User.ValidatePassword → HashUpgraded - UserName={UserName}", userName);
            }

            var userDto = _mapper.ToDetailDto(entity);
            return Result<UserDetailDto>.Success(userDto);
        }

        /// <summary>
        /// 更改密码
        /// </summary>
        public async Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            // S2: 新密码策略验证
            if (!PasswordPolicyValidator.Validate(newPassword, out var policyErrors))
            {
                return Result.Failure(string.Join("; ", policyErrors));
            }

            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result.Failure(GenericErrorCode.UserNotFound);

            // 验证旧密码并获取验证结果
            var verificationResult = PasswordHelper.VerifyPassword(oldPassword, entity.PasswordHash, entity.Role, _logger);
            if (!verificationResult.IsSuccess)
                return Result.Failure(GenericErrorCode.InvalidPassword, "原密码错误");

            // S1-fix: 始终使用新密码哈希，而非旧密码的 rehash
            // verificationResult.NewHashedPassword 是旧密码的新算法哈希（用于升级场景），
            // 在修改密码时必须使用 newPassword 哈希，否则新密码会被丢弃
            entity.PasswordHash = PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
            await _repository.UpdateAsync(entity);

            // X3-05: 修改密码后撤销所有 Token，强制重新登录
            await _authService.RevokeUserTokensAsync(id, "密码已修改，强制重新登录");

            return Result.Success();
        }

        /// <summary>
        /// 修改个人信息 (Issue #1888)
        /// </summary>
        public async Task<Result<UserDetailDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理

            // 验证输入
            if (dto == null)
            {
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "个人资料信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.RealName))
            {
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "真实姓名不能为空");
            }

            // 获取用户实体
            var entity = await _repository.GetByIdAsync(userId);
            if (entity == null)
            {
                _logger.LogWarning("[SVC] User.ChangeProfile → NotFound - UserId={UserId}", userId);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);
            }

            // 更新字段
            entity.RealName = dto.RealName;
            entity.PhoneNumber = dto.PhoneNumber;

            // 保存更改
            var updatedEntity = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.ToDetailDto(updatedEntity);

            _logger.LogInformation("[SVC] User.ChangeProfile completed - UserId={UserId} RealName={RealName}", userId, dto.RealName);
            return Result<UserDetailDto>.Success(resultDto);
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
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            // 权限检查
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, entity.Role))
            {
                _logger.LogWarning("[SVC] User.ToggleStatus → PermissionDenied - CurrentRole={CurrentRole} UserId={UserId} TargetRole={TargetRole}",
                    currentRole, id, entity.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限修改该用户状态");
            }

            // S2-07: 最后管理员保护 - 禁用管理员级用户前检查是否是最后一个
            if (entity.Status == CommonStatus.Enabled && entity.Role >= UserRole.Admin)
            {
                var activeAdmins = await _repository.FindAsync(
                    u => u.Role >= UserRole.Admin && u.Status == CommonStatus.Enabled);
                if (activeAdmins.Count() <= 1)
                {
                    _logger.LogWarning("[SVC] User.ToggleStatus → LastAdminProtection - UserId={UserId} Role={Role}", id, entity.Role);
                    return Result<UserDetailDto>.Failure(GenericErrorCode.CannotDeleteSysAdmin, "不能禁用最后一个管理员");
                }
            }

            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            // X3-06: 禁用用户时撤销所有 Token
            if (entity.Status == CommonStatus.Disabled)
            {
                await _authService.RevokeUserTokensAsync(id, "用户已禁用，强制登出");
            }

            _logger.LogInformation("[SVC] User.ToggleStatus completed - UserId={UserId} Status={Status}", id, entity.Status);
            return Result<UserDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的用户
        /// </summary>
        public async Task<Result<UserDetailDto>> RestoreAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            if (!entity.IsDeleted)
                return Result<UserDetailDto>.Failure(GenericErrorCode.InvalidRequest, "该用户未被删除，无需恢复");

            // 权限检查
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, entity.Role))
            {
                _logger.LogWarning("[SVC] User.Restore → PermissionDenied - CurrentRole={CurrentRole} UserId={UserId} TargetRole={TargetRole}",
                    currentRole, id, entity.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限恢复该用户");
            }

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] User.Restore completed - UserId={UserId} UserName={UserName}", id, entity.UserName);
            return Result<UserDetailDto>.Success(dto);
        }


        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc />
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid? currentUserId = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // ERR-012: 修复ex.Message暴露

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count
            };

            if (ids.Count == 0)
            {
                return Result<BatchOperationResultDto>.Failure(GenericErrorCode.ValidationFailed, "请至少选择一个用户");
            }

            var currentRole = GetCurrentUserRole();

            foreach (var id in ids)
            {
                // 不能删除自己
                if (currentUserId.HasValue && id == currentUserId.Value)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "不能删除自己"
                    });
                    result.FailureCount++;
                    continue;
                }

                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "用户不存在"
                    });
                    result.FailureCount++;
                    continue;
                }

                // 权限检查
                var permissionCheck = await CanDeleteUserAsync(id, user.Role);
                if (!permissionCheck.IsSuccess)
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = permissionCheck.Message ?? "无权限删除"
                    });
                    result.FailureCount++;
                    continue;
                }

                // 执行删除
                var deleteResult = await _repository.DeleteAsync(id);
                if (deleteResult)
                {
                    result.SuccessCount++;
                    _logger.LogInformation("[SVC] User.BatchDelete → ItemSuccess - UserId={UserId} UserName={UserName}", id, user.UserName);
                }
                else
                {
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = "删除操作失败"
                    });
                    result.FailureCount++;
                }
            }

            _logger.LogInformation("[SVC] User.BatchDelete completed - TotalCount={Total} SuccessCount={Success} FailureCount={Failure}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return Result<BatchOperationResultDto>.Success(result);
        }


        /// <inheritdoc />
        public async Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status, Guid? currentUserId = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // ERR-012: 修复ex.Message暴露

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count
            };
            var statusText = status == CommonStatus.Enabled ? "启用" : "禁用";
            var currentRole = GetCurrentUserRole();

            foreach (var id in ids)
            {
                // 不能修改自己的状态
                if (currentUserId.HasValue && id == currentUserId.Value)
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = $"不能{statusText}当前登录用户"
                    });
                    continue;
                }

                var user = await _repository.GetByIdAsync(id);
                if (user == null || user.IsDeleted)
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "用户不存在"
                    });
                    continue;
                }

                // S2-08: 权限检查 - 逐个校验 CanManageUser
                if (!CanManageUser(currentRole, user.Role))
                {
                    result.FailureCount++;
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Name = user.UserName,
                        Reason = $"无权限{statusText}该用户"
                    });
                    continue;
                }

                // S2-08: 最后管理员保护 (禁用场景)
                if (status == CommonStatus.Disabled
                    && user.Status == CommonStatus.Enabled
                    && user.Role >= UserRole.Admin)
                {
                    var activeAdmins = await _repository.FindAsync(
                        u => u.Role >= UserRole.Admin && u.Status == CommonStatus.Enabled);
                    if (activeAdmins.Count() <= 1)
                    {
                        result.FailureCount++;
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Name = user.UserName,
                            Reason = "不能禁用最后一个管理员"
                        });
                        continue;
                    }
                }

                user.Status = status;
                user.UpdatedAt = DateTime.Now;
                await _repository.UpdateAsync(user);
                result.SuccessCount++;

                // X3: 禁用用户时撤销所有 Token
                if (status == CommonStatus.Disabled)
                {
                    await _authService.RevokeUserTokensAsync(id, "批量禁用，强制登出");
                }

                _logger.LogInformation("[SVC] User.BatchUpdateStatus → ItemSuccess - UserId={UserId} Status={Status}", id, status);
            }

            result.Message = $"批量{statusText}完成: 成功 {result.SuccessCount} 个, 失败 {result.FailureCount} 个";

            _logger.LogInformation("[SVC] User.BatchUpdateStatus completed - Total={Total} Success={Success} Failure={Failure}",
                result.TotalCount, result.SuccessCount, result.FailureCount);

            return Result<BatchOperationResultDto>.Success(result);
        }
    }
}
