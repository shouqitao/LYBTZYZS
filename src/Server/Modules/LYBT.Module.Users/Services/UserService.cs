using System.Security.Claims;
using System.Threading;
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
        private readonly IUserBatchOperationService _batchService;
        private readonly LYBT.Module.Registration.Interfaces.IRegistrationRepository _registrationRepository;
        private readonly IUserQueryService _queryService;
        private readonly IUserPasswordService _passwordService;
        private readonly IUserStatusService _statusService;
        private readonly UserMapper _mapper = new();

        public UserService(
            IUserRepository repository,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IValidator<UserInputDto> validator,
            ICrossModuleAuthService authService,
            IUserBatchOperationService batchService,
            LYBT.Module.Registration.Interfaces.IRegistrationRepository registrationRepository,
            IUserQueryService queryService,
            IUserPasswordService passwordService,
            IUserStatusService statusService)
            : base(logger)
        {
            _repository = repository;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
            _authService = authService;
            _batchService = batchService;
            _registrationRepository = registrationRepository;
            _queryService = queryService;
            _passwordService = passwordService;
            _statusService = statusService;
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
        /// - SuperAdmin（100）可以管理所有用户（Admin、Doctor、Receptionist）
        /// - Admin（10）可以管理 Doctor 和 Receptionist，但不能管理 Admin 或 SuperAdmin
        /// - Doctor（1）不能管理其他用户
        /// - Receptionist（0）不能管理其他用户
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
        private async Task<Result> CanDeleteUserAsync(Guid userId, UserRole targetRole, CancellationToken cancellationToken = default)
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
                var users = await _repository.FindAsync(u => u.Role == targetRole, cancellationToken);
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
        public Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null,
            CancellationToken cancellationToken = default)
            => _queryService.GetPagedAsync(page, pageSize, keyword, role, status, cancellationToken);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => _queryService.GetByIdAsync(id, cancellationToken);

        /// <summary>
        /// 搜索用户（返回所有匹配结果）
        /// </summary>
        public Task<Result<List<UserListDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
            => _queryService.SearchAsync(keyword, cancellationToken);

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
            var existingUser = await _repository.UsernameExistsAsync(dto.UserName!, cancellationToken);
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
                // 从配置读取默认密码：DefaultPasswords:NewUserPassword
                passwordToHash = _configuration["DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";
                _logger.LogInformation("[SVC] User.Create → DefaultPassword - UserName={UserName}", dto.UserName);
            }

            // Issue #2547: 使用统一PasswordHelper进行密码哈希
            entity.PasswordHash = PasswordHelper.HashPassword(passwordToHash, entity.Role, _logger);

            var result = await _repository.AddAsync(entity, cancellationToken);
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
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
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

            // USER-D05 / CODE-04: sysadmin 硬兜底
            if (IsSysAdmin(entity))
            {
                _logger.LogWarning("[SVC] User.Update → SysAdminProtection - UserId={UserId}", id);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "系统管理员账号不可被修改");
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

            var result = await _repository.UpdateAsync(entity, cancellationToken);
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
        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
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
            var targetUser = await _repository.GetByIdAsync(id, cancellationToken);
            if (targetUser == null)
                return Result.Failure(GenericErrorCode.UserNotFound);

            // USER-D05 / CODE-04: sysadmin 硬兜底
            if (IsSysAdmin(targetUser))
            {
                _logger.LogWarning("[SVC] User.Delete → SysAdminProtection - UserId={UserId}", id);
                return Result.Failure(GenericErrorCode.Forbidden, "系统管理员账号不可被删除");
            }

            // Issue #1909: 权限检查和保护逻辑
            var permissionCheck = await CanDeleteUserAsync(id, targetUser.Role, cancellationToken);
            if (!permissionCheck.IsSuccess)
            {
                _logger.LogWarning("[SVC] User.Delete → PermissionDenied - UserId={UserId} Reason={Reason}",
                    id, permissionCheck.Message);
                return permissionCheck;
            }

            // X3-03: 删除前撤销所有 Token
            await _authService.RevokeUserTokensAsync(id, "用户已删除");

            var result = await _repository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("[SVC] User.Delete completed - UserId={UserId} Role={Role}", id, targetUser.Role);
            return result ? Result.Success() : Result.Failure(GenericErrorCode.InternalError, "删除失败");
        }

        /// <summary>
        /// 管理员重置密码（Issue #1162: 使用配置文件中的默认密码）
        /// 修复：重置密码不再接受新密码参数，始终使用配置文件中的默认密码
        /// </summary>
        public Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
            => _passwordService.ResetPasswordAsync(id, request, cancellationToken);

        /// <summary>
        /// 验证用户密码
        /// Issue #1864: Auth/User职责分离，密码验证由UserService负责
        /// </summary>
        public Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password, CancellationToken cancellationToken = default)
            => _passwordService.ValidatePasswordAsync(userName, password, cancellationToken);

        /// <summary>
        /// 更改密码
        /// </summary>
        public Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
            => _passwordService.ChangePasswordAsync(id, oldPassword, newPassword, cancellationToken);

        /// <summary>
        /// 修改个人信息 (Issue #1888)
        /// </summary>
        public async Task<Result<UserDetailDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken cancellationToken = default)
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
            var entity = await _repository.GetByIdAsync(userId, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[SVC] User.ChangeProfile → NotFound - UserId={UserId}", userId);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);
            }

            // 更新字段
            // T5-P2-32: RealName 变更时重新生成拼音码
            if (!string.Equals(entity.RealName, dto.RealName, StringComparison.Ordinal))
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(dto.RealName);
            }
            entity.RealName = dto.RealName;
            entity.PhoneNumber = dto.PhoneNumber;

            // T5-P3-19: 更新 Email (仅当 DTO 提供了值时)
            if (dto.Email != null)
            {
                entity.Email = dto.Email;
            }

            // 保存更改
            var updatedEntity = await _repository.UpdateAsync(entity, cancellationToken);
            var resultDto = _mapper.ToDetailDto(updatedEntity);

            _logger.LogInformation("[SVC] User.ChangeProfile completed - UserId={UserId} RealName={RealName}", userId, dto.RealName);
            return Result<UserDetailDto>.Success(resultDto);
        }

        #endregion

        #region 私有辅助方法

        // Issue #1757: GenerateTemporaryPassword已移至PasswordHelper.GenerateTemporaryPassword

        /// <summary>
        /// USER-D05 / CODE-04: sysadmin 账户硬兜底检查。
        /// sysadmin 不可被任何人管理 (不可修改角色/删除/禁用/重置密码)。
        /// 自助操作 (/profile, /change-password) 不受此限制。
        /// </summary>
        private static bool IsSysAdmin(User user) => user.UserName == "sysadmin";

        #endregion

        public Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
            => _statusService.ToggleStatusAsync(id, cancellationToken);

        public Task<Result<UserDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
            => _statusService.RestoreAsync(id, cancellationToken);


        // ========== 批量操作委托给 IUserBatchOperationService ==========

        /// <inheritdoc />
        public Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid? currentUserId = null, CancellationToken cancellationToken = default)
            => _batchService.BatchDeleteAsync(ids, currentUserId, cancellationToken);

        /// <inheritdoc />
        public Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status, Guid? currentUserId = null, CancellationToken cancellationToken = default)
            => _batchService.BatchUpdateStatusAsync(ids, status, currentUserId, cancellationToken);
    }
}
