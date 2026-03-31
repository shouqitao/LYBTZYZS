using System.Security.Claims;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Users.Services
{
    public class UserStatusService : IUserStatusService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserStatusService> _logger;
        private readonly ICrossModuleAuthService _authService;
        private readonly LYBT.Module.Registration.Interfaces.IRegistrationRepository _registrationRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserMapper _mapper = new();

        public UserStatusService(
            IUserRepository repository,
            ILogger<UserStatusService> logger,
            ICrossModuleAuthService authService,
            LYBT.Module.Registration.Interfaces.IRegistrationRepository registrationRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _logger = logger;
            _authService = authService;
            _registrationRepository = registrationRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            if (IsSysAdmin(entity))
            {
                _logger.LogWarning("[SVC] User.ToggleStatus → SysAdminProtection - UserId={UserId}", id);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "系统管理员账号不可被禁用");
            }

            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, entity.Role))
            {
                _logger.LogWarning("[SVC] User.ToggleStatus → PermissionDenied - CurrentRole={CurrentRole} UserId={UserId} TargetRole={TargetRole}",
                    currentRole, id, entity.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限修改该用户状态");
            }

            if (entity.Status == CommonStatus.Enabled && entity.Role >= UserRole.Admin)
            {
                var activeAdmins = await _repository.FindAsync(
                    u => u.Role >= UserRole.Admin && u.Status == CommonStatus.Enabled, cancellationToken);
                if (activeAdmins.Count() <= 1)
                {
                    _logger.LogWarning("[SVC] User.ToggleStatus → LastAdminProtection - UserId={UserId} Role={Role}", id, entity.Role);
                    return Result<UserDetailDto>.Failure(GenericErrorCode.CannotDeleteSysAdmin, "不能禁用最后一个管理员");
                }
            }

            if (entity.Status == CommonStatus.Enabled && entity.Role == UserRole.Doctor)
            {
                var waitingCount = await _registrationRepository.GetWaitingCountByDoctorAsync(id, cancellationToken);
                if (waitingCount > 0)
                {
                    _logger.LogWarning("[SVC] User.ToggleStatus → DoctorHasWaitingRegistrations - UserId={UserId} Count={Count}", id, waitingCount);
                    return Result<UserDetailDto>.Failure(
                        GenericErrorCode.RegistrationDoctorHasWaiting,
                        $"该医生有 {waitingCount} 条等待中的挂号记录，请先由前台取消后再禁用");
                }
            }

            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity, cancellationToken);
            var dto = _mapper.ToDetailDto(result);

            if (entity.Status == CommonStatus.Disabled)
            {
                await _authService.RevokeUserTokensAsync(id, "用户已禁用，强制登出");
            }

            _logger.LogInformation("[SVC] User.ToggleStatus completed - UserId={UserId} Status={Status}", id, entity.Status);
            return Result<UserDetailDto>.Success(dto);
        }

        public async Task<Result<UserDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            if (IsSysAdmin(entity))
            {
                _logger.LogWarning("[SVC] User.Restore → SysAdminProtection - UserId={UserId}", id);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "系统管理员账号不可被管理");
            }

            if (!entity.IsDeleted)
                return Result<UserDetailDto>.Failure(GenericErrorCode.InvalidRequest, "该用户未被删除，无需恢复");

            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, entity.Role))
            {
                _logger.LogWarning("[SVC] User.Restore → PermissionDenied - CurrentRole={CurrentRole} UserId={UserId} TargetRole={TargetRole}",
                    currentRole, id, entity.Role);
                return Result<UserDetailDto>.Failure(GenericErrorCode.Forbidden, "您没有权限恢复该用户");
            }

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity, cancellationToken);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] User.Restore completed - UserId={UserId} UserName={UserName}", id, entity.UserName);
            return Result<UserDetailDto>.Success(dto);
        }

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

        private static bool CanManageUser(UserRole? currentUserRole, UserRole? targetUserRole)
        {
            if (!currentUserRole.HasValue || !targetUserRole.HasValue)
                return false;

            return currentUserRole.Value switch
            {
                UserRole.SuperAdmin => true,
                UserRole.Admin => targetUserRole.Value is UserRole.Doctor or UserRole.Receptionist,
                UserRole.Doctor => false,
                _ => false
            };
        }

        private static bool IsSysAdmin(User user) => user.UserName == "sysadmin";
    }
}
