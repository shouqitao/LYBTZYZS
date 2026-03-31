using LYBT.Entities.Users;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Users.Services
{
    public class UserPasswordService : IUserPasswordService
    {
        private readonly IUserRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ICrossModuleAuthService _authService;
        private readonly ILogger<UserPasswordService> _logger;
        private readonly UserMapper _mapper = new();

        public UserPasswordService(
            IUserRepository repository,
            IConfiguration configuration,
            ICrossModuleAuthService authService,
            ILogger<UserPasswordService> logger)
        {
            _repository = repository;
            _configuration = configuration;
            _authService = authService;
            _logger = logger;
        }

        public async Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return Result<ResetPasswordResponseDto>.Failure(GenericErrorCode.UserNotFound);

            if (IsSysAdmin(entity))
            {
                _logger.LogWarning("[SVC] User.ResetPassword → SysAdminProtection - UserId={UserId}", id);
                return Result<ResetPasswordResponseDto>.Failure(GenericErrorCode.Forbidden, "系统管理员账号密码不可被重置");
            }

            string password = _configuration["DefaultPasswords:NewUserPassword"]
                ?? PasswordHelper.GenerateTemporaryPassword();

            entity.PasswordHash = PasswordHelper.HashPassword(password, entity.Role, _logger);
            entity.MustChangeOnNextLogin = true;
            await _repository.UpdateAsync(entity, cancellationToken);

            await _authService.RevokeUserTokensAsync(id, "密码已重置，强制重新登录");

            var response = new ResetPasswordResponseDto
            {
                Success = true,
                TemporaryPassword = password
            };

            _logger.LogInformation("[SVC] User.ResetPassword completed - UserId={UserId}", id);

            return Result<ResetPasswordResponseDto>.Success(response);
        }

        public async Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "用户名不能为空");

            if (string.IsNullOrWhiteSpace(password))
                return Result<UserDetailDto>.Failure(GenericErrorCode.ValidationFailed, "密码不能为空");

            var entity = await _repository.GetByUsernameAsync(userName, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → NotFound - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            if (entity.Status == CommonStatus.Disabled)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → Disabled - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserDisabled, "用户已被禁用");
            }

            var verificationResult = PasswordHelper.VerifyPassword(password, entity.PasswordHash, entity.Role, _logger);
            if (!verificationResult.IsSuccess)
            {
                _logger.LogWarning("[SVC] User.ValidatePassword → InvalidPassword - UserName={UserName}", userName);
                return Result<UserDetailDto>.Failure(GenericErrorCode.AuthInvalidCredentials, "用户名或密码错误");
            }

            if (verificationResult.NewHashedPassword != null)
            {
                entity.PasswordHash = verificationResult.NewHashedPassword;
                await _repository.UpdateAsync(entity, cancellationToken);
                _logger.LogInformation("[SVC] User.ValidatePassword → HashUpgraded - UserName={UserName}", userName);
            }

            var userDto = _mapper.ToDetailDto(entity);
            return Result<UserDetailDto>.Success(userDto);
        }

        public async Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            if (!PasswordPolicyValidator.Validate(newPassword, out var policyErrors))
            {
                return Result.Failure(string.Join("; ", policyErrors));
            }

            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return Result.Failure(GenericErrorCode.UserNotFound);

            var verificationResult = PasswordHelper.VerifyPassword(oldPassword, entity.PasswordHash, entity.Role, _logger);
            if (!verificationResult.IsSuccess)
                return Result.Failure(GenericErrorCode.InvalidPassword, "原密码错误");

            entity.PasswordHash = PasswordHelper.HashPassword(newPassword, entity.Role, _logger);
            entity.MustChangeOnNextLogin = false;
            await _repository.UpdateAsync(entity, cancellationToken);

            await _authService.RevokeUserTokensAsync(id, "密码已修改，强制重新登录");

            return Result.Success();
        }

        private static bool IsSysAdmin(User user) => user.UserName == "sysadmin";
    }
}
