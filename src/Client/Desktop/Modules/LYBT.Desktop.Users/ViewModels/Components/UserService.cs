using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.ViewModels.Components
{
    /// <summary>
    /// 用户Service - 组件化架构实现
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// Issue #1785: 负责用户的命令操作（创建、更新、删除、密码管理等）
    /// </summary>
    public class UserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基本CRUD操作

        /// <summary>
        /// 创建用户
        /// OpenSpec: dto-architecture-specification - 统一使用UserDetailDto
        /// </summary>
        public virtual async Task<(bool success, UserDetailDto? user, string? errorMessage)> CreateAsync(UserInputDto createDto)
        {
            try
            {
                _logger.LogInformation("创建用户: {Username}", createDto.UserName);

                var createdUser = await _repository.CreateAsync(createDto);
                _logger.LogInformation("用户创建成功: {UserId}", createdUser.Id);

                return (true, createdUser, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户时发生异常: {Username}", createDto.UserName);
                return (false, null, "创建用户时发生系统错误");
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public virtual async Task<(bool success, UserDetailDto? user, string? errorMessage)> UpdateAsync(UserInputDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新用户: {UserId}", updateDto.Id);

                var updatedUser = await _repository.UpdateAsync(updateDto);
                _logger.LogInformation("用户更新成功: {Username}", updatedUser.UserName);

                return (true, updatedUser, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户时发生异常: {UserId}", updateDto.Id);
                return (false, null, "更新用户时发生系统错误");
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public virtual async Task<(bool success, string? errorMessage)> DeleteAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("删除用户: {UserId}", userId);

                var result = await _repository.DeleteAsync(userId);

                if (result)
                {
                    _logger.LogInformation("用户删除成功");
                    return (true, null);
                }
                else
                {
                    _logger.LogWarning("用户删除失败：{UserId}", userId);
                    return (false, "删除用户失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户时发生异常: {UserId}", userId);
                return (false, "删除用户时发生系统错误");
            }
        }


        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc />
        public virtual async Task<(bool success, BatchOperationResultDto? result, string? errorMessage)> BatchDeleteAsync(List<Guid> userIds)
        {
            try
            {
                _logger.LogInformation("批量删除用户：{Count} 个", userIds.Count);

                var result = await _repository.BatchDeleteAsync(userIds);

                if (result != null)
                {
                    _logger.LogInformation("批量删除用户完成：成功 {Success} 个，失败 {Failure} 个",
                        result.SuccessCount, result.FailureCount);
                    return (true, result, null);
                }
                else
                {
                    _logger.LogWarning("批量删除用户失败");
                    return (false, null, "批量删除用户失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除用户时发生异常");
                return (false, null, "批量删除用户时发生系统错误");
            }
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        public virtual async Task<(bool success, UserDetailDto? user, string? errorMessage)> GetByIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("开始查询用户: UserId={UserId}", userId);

                var user = await _repository.GetByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("用户不存在：UserId={UserId}", userId);
                    return (false, null, "用户不存在");
                }

                _logger.LogInformation("查询用户成功：{Username}", user.UserName);
                return (true, user, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询用户时发生异常：UserId={UserId}", userId);
                return (false, null, "查询用户时发生系统错误");
            }
        }

        /// <summary>
        /// 分页查询用户（返回轻量级ListDto）
        /// </summary>
        public async Task<(bool success, PagedResult<UserListDto>? data, string? errorMessage)> GetPagedAsync(
            int page, int pageSize, string? searchText = null)
        {
            try
            {
                _logger.LogInformation("分页查询用户: Page={Page}, PageSize={PageSize}, SearchText={SearchText}",
                    page, pageSize, searchText);

                var result = await _repository.GetPagedAsync(page, pageSize, searchText);

                _logger.LogInformation("查询成功，共{TotalCount}条数据", result.TotalCount);
                return (true, result, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询用户时发生异常");
                return (false, null, "查询用户时发生系统错误");
            }
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<(bool success, UserDetailDto? user, string? errorMessage)> GetByUsernameAsync(string username)
        {
            try
            {
                _logger.LogInformation("根据用户名查询用户: {Username}", username);

                var user = await _repository.GetByUsernameAsync(username);

                if (user == null)
                {
                    _logger.LogWarning("用户不存在：{Username}", username);
                    return (false, null, "用户不存在");
                }

                _logger.LogInformation("查询用户成功：{Username}", user.UserName);
                return (true, user, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户名查询用户时发生异常：{Username}", username);
                return (false, null, "查询用户时发生系统错误");
            }
        }

        /// <summary>
        /// 搜索用户（返回轻量级ListDto）
        /// </summary>
        public async Task<(bool success, List<UserListDto>? users, string? errorMessage)> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索用户: {Keyword}", keyword);

                var users = await _repository.SearchAsync(keyword);

                _logger.LogInformation("搜索成功，找到{Count}个用户", users.Count);
                return (true, users, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索用户时发生异常");
                return (false, null, "搜索用户时发生系统错误");
            }
        }

        /// <summary>
        /// 获取医生列表（返回轻量级ListDto）
        /// </summary>
        public async Task<(bool success, List<UserListDto>? doctors, string? errorMessage)> GetDoctorsAsync()
        {
            try
            {
                _logger.LogInformation("查询医生列表");

                var doctors = await _repository.GetDoctorsAsync();

                _logger.LogInformation("查询成功，共{Count}名医生", doctors.Count);
                return (true, doctors, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询医生列表时发生异常");
                return (false, null, "查询医生列表时发生系统错误");
            }
        }

        #endregion

        #region 个人资料管理

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        public async Task<(bool success, UserDetailDto? user, string? errorMessage)> ChangeProfileAsync(
            Guid userId, ChangeProfileDto dto)
        {
            try
            {
                _logger.LogInformation("修改个人资料: UserId={UserId}", userId);

                var updatedUser = await _repository.ChangeProfileAsync(userId, dto);

                _logger.LogInformation("个人资料修改成功");
                return (true, updatedUser, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人资料时发生异常: UserId={UserId}", userId);
                return (false, null, "修改个人资料时发生系统错误");
            }
        }

        #endregion

        #region 密码管理（占位实现）

        /// <summary>
        /// 修改密码（占位实现 - 实际应该调用认证服务）
        /// </summary>
        public Task<(bool success, string? errorMessage)> ChangePasswordAsync(
            Guid userId, string oldPassword, string newPassword)
        {
            _logger.LogInformation("修改密码: {UserId}", userId);

            // TODO: 实现修改密码逻辑（应该调用认证服务）
            return Task.FromResult<(bool, string?)>((true, "修改密码功能开发中"));
        }

        /// <summary>
        /// 重置密码（占位实现 - 实际应该调用认证服务）
        /// </summary>
        /// <summary>
        /// 重置用户密码（管理员操作）(Issue #1911)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码（明文）</param>
        /// <returns>成功标志、错误信息、重置响应数据</returns>
        public async Task<(bool success, string? errorMessage, ResetPasswordResponseDto? response)> ResetPasswordAsync(
            Guid userId,
            string newPassword)
        {
            try
            {
                _logger.LogInformation("UserService: 开始重置密码, UserId: {UserId}", userId);

                // 构建请求DTO
                var request = new ResetPasswordRequestDto();

                // 调用Repository
                var result = await _repository.ResetPasswordAsync(userId, request);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation("UserService: 重置密码成功, UserId: {UserId}", userId);
                    return (true, null, result.Data);
                }
                else
                {
                    _logger.LogWarning("UserService: 重置密码失败, UserId: {UserId}, Message: {Message}",
                        userId, result.Message);
                    return (false, result.Message, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserService: 重置密码异常, UserId: {UserId}", userId);
                return (false, ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex), null);
            }
        }

        #endregion
    }
}
