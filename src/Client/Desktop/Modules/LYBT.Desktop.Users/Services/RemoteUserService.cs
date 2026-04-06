using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using System.Threading;
using Refit;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// 用户Remote Service实现
    /// 通过 IUserRepository 调用远程API
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// </summary>
    public class RemoteUserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RemoteUserService> _logger;

        public RemoteUserService(
            IUserRepository userRepository,
            ILogger<RemoteUserService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基本CRUD操作

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> CreateAsync(UserInputDto createDto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.Create started - UserName={UserName}", createDto.UserName);

                var user = await _userRepository.CreateAsync(createDto);
                _logger.LogInformation("[SVC] User.Create completed - UserId={UserId}", user.Id);
                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.Create failed - UserName={UserName}", createDto.UserName);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("创建用户", ex));
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> UpdateAsync(UserInputDto updateDto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.Update started - UserId={UserId}", updateDto.Id);

                var user = await _userRepository.UpdateAsync(updateDto);
                _logger.LogInformation("[SVC] User.Update completed - UserId={UserId}", user.Id);
                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.Update failed - UserId={UserId}", updateDto.Id);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("更新用户", ex));
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task<CommandResult<bool>> DeleteAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.Delete started - UserId={UserId}", userId);

                await _userRepository.DeleteAsync(userId);
                _logger.LogInformation("[SVC] User.Delete completed - UserId={UserId}", userId);
                return CommandResult<bool>.Succeeded(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.Delete failed - UserId={UserId}", userId);
                return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("删除用户", ex));
            }
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> userIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.BatchDelete started - Count={Count}", userIds.Count);

                var result = await _userRepository.BatchDeleteAsync(userIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量删除用户返回空结果");

                _logger.LogInformation("[SVC] User.BatchDelete completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.BatchDelete failed - Count={Count}", userIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量删除用户", ex));
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.GetById - UserId={UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return CommandResult<UserDetailDto>.NotFound("用户不存在");

                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.GetById failed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取用户", ex));
            }
        }

        /// <summary>
        /// 分页查询用户
        /// </summary>
        public async Task<CommandResult<PagedResult<UserListDto>>> GetPagedAsync(
            int page, int pageSize, string? searchText = null, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.GetPaged - Page={Page}, PageSize={PageSize}, Search={Search}",
                    page, pageSize, searchText);

                var result = await _userRepository.GetPagedAsync(page, pageSize, searchText);
                return CommandResult<PagedResult<UserListDto>>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.GetPaged failed - Page={Page}, Search={Search}", page, searchText);
                return CommandResult<PagedResult<UserListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("分页查询用户", ex));
            }
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        public async Task<CommandResult<List<UserDetailDto>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.GetAll started");

                // IUserRepository has no GetAllAsync(); use GetPagedAsync with large page size
                var pagedResult = await _userRepository.GetPagedAsync(1, int.MaxValue);
                var users = new List<UserDetailDto>();
                foreach (var item in pagedResult.Items)
                {
                    var detail = await _userRepository.GetByIdAsync(item.Id);
                    if (detail != null)
                        users.Add(detail);
                }

                _logger.LogDebug("[SVC] User.GetAll completed - Count={Count}", users.Count);
                return CommandResult<List<UserDetailDto>>.Succeeded(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.GetAll failed");
                return CommandResult<List<UserDetailDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取所有用户", ex));
            }
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.GetByUsername - UserName={UserName}", username);

                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                    return CommandResult<UserDetailDto>.NotFound("用户不存在");

                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.GetByUsername failed - UserName={UserName}", username);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("根据用户名获取用户", ex));
            }
        }

        /// <summary>
        /// 搜索用户
        /// </summary>
        public async Task<CommandResult<List<UserListDto>>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.Search - Keyword={Keyword}", keyword);

                var users = await _userRepository.SearchAsync(keyword);
                _logger.LogDebug("[SVC] User.Search completed - Count={Count}", users.Count);
                return CommandResult<List<UserListDto>>.Succeeded(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.Search failed - Keyword={Keyword}", keyword);
                return CommandResult<List<UserListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("搜索用户", ex));
            }
        }

        /// <summary>
        /// 获取医生列表
        /// </summary>
        public async Task<CommandResult<List<UserListDto>>> GetDoctorsAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("[SVC] User.GetDoctors started");

                var doctors = await _userRepository.GetDoctorsAsync();
                _logger.LogDebug("[SVC] User.GetDoctors completed - Count={Count}", doctors.Count);
                return CommandResult<List<UserListDto>>.Succeeded(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.GetDoctors failed");
                return CommandResult<List<UserListDto>>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("获取医生列表", ex));
            }
        }

        #endregion

        #region 个人资料管理

        /// <summary>
        /// 修改个人资料
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> ChangeProfileAsync(
            Guid userId, ChangeProfileDto dto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.ChangeProfile started - UserId={UserId}", userId);

                var user = await _userRepository.ChangeProfileAsync(userId, dto);
                _logger.LogInformation("[SVC] User.ChangeProfile completed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.ChangeProfile failed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改个人资料", ex));
            }
        }

        #endregion

        #region 密码管理

        /// <summary>
        /// 修改密码
        /// </summary>
        public async Task<CommandResult<bool>> ChangePasswordAsync(
            Guid userId, string oldPassword, string newPassword, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.ChangePassword started - UserId={UserId}", userId);

                var request = new ChangePasswordRequest
                {
                    OldPassword = oldPassword,
                    NewPassword = newPassword
                };
                var serviceResult = await _userRepository.ChangePasswordAsync(userId, request);

                if (serviceResult.IsSuccess)
                {
                    _logger.LogInformation("[SVC] User.ChangePassword completed - UserId={UserId}", userId);
                    return CommandResult<bool>.Succeeded(true);
                }
                else
                {
                    _logger.LogWarning("[SVC] User.ChangePassword failed - UserId={UserId}, Error={Error}", userId, serviceResult.ErrorMessage);
                    return CommandResult<bool>.Failed(serviceResult.ErrorMessage ?? "修改密码失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.ChangePassword failed - UserId={UserId}", userId);
                return CommandResult<bool>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        public async Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId, string newPassword, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.ResetPassword started - UserId={UserId}", userId);

                var request = new ResetPasswordRequestDto
                {
                    MustChangeOnNextLogin = true
                };
                var serviceResult = await _userRepository.ResetPasswordAsync(userId, request);

                if (serviceResult.IsSuccess)
                {
                    _logger.LogInformation("[SVC] User.ResetPassword completed - UserId={UserId}", userId);
                    return CommandResult<ResetPasswordResponseDto>.Succeeded(serviceResult.Data!);
                }
                else
                {
                    _logger.LogWarning("[SVC] User.ResetPassword failed - UserId={UserId}, Error={Error}", userId, serviceResult.ErrorMessage);
                    return CommandResult<ResetPasswordResponseDto>.Failed(serviceResult.ErrorMessage ?? "重置密码失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.ResetPassword failed - UserId={UserId}", userId);
                return CommandResult<ResetPasswordResponseDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex));
            }
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 切换用户状态
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> ToggleStatusAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.ToggleStatus started - UserId={UserId}", userId);

                var user = await _userRepository.ToggleStatusAsync(userId);
                if (user == null)
                    return CommandResult<UserDetailDto>.NotFound("用户不存在");

                _logger.LogInformation("[SVC] User.ToggleStatus completed - UserId={UserId}, Status={Status}",
                    userId, user.Status);
                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.ToggleStatus failed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("切换用户状态", ex));
            }
        }

        /// <summary>
        /// 恢复已删除用户
        /// </summary>
        public async Task<CommandResult<UserDetailDto>> RestoreAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.Restore started - UserId={UserId}", userId);

                var user = await _userRepository.RestoreAsync(userId);
                if (user == null)
                    return CommandResult<UserDetailDto>.NotFound("用户不存在或未被删除");

                _logger.LogInformation("[SVC] User.Restore completed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Succeeded(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.Restore failed - UserId={UserId}", userId);
                return CommandResult<UserDetailDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("恢复用户", ex));
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> userIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.BatchEnable started - Count={Count}", userIds.Count);

                var result = await _userRepository.BatchEnableAsync(userIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量启用用户返回空结果");

                _logger.LogInformation("[SVC] User.BatchEnable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.BatchEnable failed - Count={Count}", userIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量启用用户", ex));
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> userIds, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.BatchDisable started - Count={Count}", userIds.Count);

                var result = await _userRepository.BatchDisableAsync(userIds);
                if (result == null)
                    return CommandResult<BatchOperationResultDto>.Failed("批量禁用用户返回空结果");

                _logger.LogInformation("[SVC] User.BatchDisable completed - Success={Success}, Failed={Failed}",
                    result.SuccessCount, result.FailureCount);
                return CommandResult<BatchOperationResultDto>.Succeeded(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.BatchDisable failed - Count={Count}", userIds.Count);
                return CommandResult<BatchOperationResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量禁用用户", ex));
            }
        }

        /// <summary>
        /// 批量导入用户
        /// </summary>
        public async Task<CommandResult<UserBatchImportResultDto>> BatchImportAsync(StreamPart file, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("[SVC] User.BatchImport started - FileName={FileName}", file.FileName);

                // TODO: Phase 1 T1-5 - StreamPart -> UserBatchImportInputDto conversion not yet implemented
                throw new NotSupportedException(
                    "用户批量导入功能尚未完成：需要实现 StreamPart 到 UserBatchImportInputDto 的转换逻辑。");
            }
            catch (NotSupportedException ex)
            {
                _logger.LogWarning(ex, "[SVC] User.BatchImport not supported yet");
                return CommandResult<UserBatchImportResultDto>.Failed(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] User.BatchImport failed - FileName={FileName}", file.FileName);
                return CommandResult<UserBatchImportResultDto>.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("批量导入用户", ex));
            }
        }

        #endregion
    }
}
