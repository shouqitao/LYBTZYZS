using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories
{
    /// <summary>
    /// 用户数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class UserRepository : RepositoryBase<UserDto, UserInputDto, UserInputDto, IUserApi>, IUserRepository
    {
        public UserRepository(
            IUserApi userApi,
            ILogger<UserRepository> logger)
            : base(userApi, logger)
        {
        }

        /// <summary>
        /// 获取所有用户（通过分页获取第一页的大量数据）
        /// </summary>
        public async Task<List<UserDto>> GetAllAsync()
        {
            try
            {
                var pagedResult = await GetPagedAsync(1, 1000);
                return pagedResult.Items ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有用户失败");
                return new List<UserDto>();
            }
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<UserDto> GetByUsernameAsync(string username)
        {
            try
            {
                // 由于IUserApi没有GetByUsernameAsync方法，使用搜索方式
                var searchResult = await SearchAsync(username);
                return searchResult.FirstOrDefault(u => u.UserName == username)
                    ?? throw new InvalidOperationException($"用户 {username} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"根据用户名获取用户失败: {username}");
                throw;
            }
        }

        /// <summary>
        /// 获取所有医生用户（Desktop端本地筛选实现）
        /// Issue #1155 - 使用本地角色筛选替代不存在的Server API
        /// </summary>
        public async Task<List<UserDto>> GetDoctorsAsync()
        {
            try
            {
                _logger.LogDebug("获取所有医生用户");

                // 获取所有用户（第1页，100条，足够覆盖小诊所全部用户）
                var result = await GetPagedAsync(1, 100, null);

                if (result?.Items == null)
                {
                    _logger.LogWarning("获取用户列表失败或返回空");
                    return new List<UserDto>();
                }

                // Desktop端本地筛选：角色=医生 && 状态=启用
                var doctors = result.Items
                    .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                    .ToList();

                _logger.LogInformation("成功获取{Count}名医生用户", doctors.Count);
                return doctors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生用户列表时发生异常");
                return new List<UserDto>();
            }
        }

        /// <summary>
        /// 分页获取用户列表（返回UserListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<PagedResult<UserListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("获取用户列表: Page={Page}, PageSize={PageSize}, Keyword={Keyword}", page, pageSize, keyword);

                var response = await _api.GetUsersListAsync(page, pageSize, keyword);

                if (response.Success && response.Data != null)
                {
                    _logger.LogDebug("获取用户列表成功，共{Count}条", response.Data.TotalCount);
                    return response.Data;
                }

                _logger.LogWarning("获取用户列表失败: {Message}", response.Message);
                return new PagedResult<UserListDto>
                {
                    Items = new List<UserListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表异常");
                return new PagedResult<UserListDto>
                {
                    Items = new List<UserListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<UserDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetUserByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<UserDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetUsersAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<UserDto>> CallApiCreateAsync(UserInputDto dto)
        {
            return _api.CreateUserAsync(dto);
        }

        protected override Task<ApiResponse<UserDto>> CallApiUpdateAsync(Guid id, UserInputDto dto)
        {
            return _api.UpdateUserAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteUserAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(UserInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// </summary>
        public async Task<UserDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
        {
            try
            {
                _logger.LogInformation("修改个人资料: UserId={UserId}", userId);

                var response = await _api.ChangeProfileAsync(userId, dto);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("个人资料修改成功");
                    return response.Data;
                }

                var errorMsg = response.Message ?? "修改个人资料失败";
                _logger.LogWarning("修改个人资料失败: {Message}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人资料时发生异常: UserId={UserId}", userId);
                throw;
            }
        }


        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request)
        {
            try
            {
                _logger.LogInformation("修改密码: UserId={UserId}", userId);

                var response = await _api.ChangePasswordAsync(userId, request);

                if (response.Success)
                {
                    _logger.LogInformation("密码修改成功");
                    return ServiceResult.Success();
                }

                var errorMsg = response.Message ?? "修改密码失败";
                _logger.LogWarning("修改密码失败: {Message}", errorMsg);
                return ServiceResult.Failure(errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码时发生异常: UserId={UserId}", userId);
                return ServiceResult.Failure($"修改密码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 管理员重置用户密码 (Issue #1911)
        /// </summary>
        public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId, 
            ResetPasswordRequestDto request)
        {
            try
            {
                _logger.LogDebug("Repository: 调用重置密码API, UserId: {UserId}", userId);
                
                var apiResponse = await _api.ResetPasswordAsync(userId, request);
                
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("Repository: 重置密码成功, UserId: {UserId}", userId);
                    return ServiceResult<ResetPasswordResponseDto>.Success(apiResponse.Data);
                }
                else
                {
                    _logger.LogWarning("Repository: 重置密码失败, UserId: {UserId}, Message: {Message}", 
                        userId, apiResponse.Message);
                    return ServiceResult<ResetPasswordResponseDto>.Failure(
                        apiResponse.Message ?? "重置密码失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository: 调用重置密码API异常, UserId: {UserId}", userId);
                return ServiceResult<ResetPasswordResponseDto>.Failure($"重置密码异常: {ex.Message}");
            }
        }


        /// <summary>
        /// 批量导入用户 (Issue #2003 Task 2.10)
        /// Desktop主导模式：接收Desktop解析好的DTO列表，调用Server API
        /// </summary>
        /// <summary>
        /// 批量导入用户 (Issue #2003 Task 2.10)
        /// Desktop主导模式：接收Desktop解析好的DTO列表，调用Server API
        /// </summary>
        public async Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportRequestDto request)
        {
            try
            {
                // 调用Server端API: POST /api/v1/users/batch-import
                var response = await _api.BatchImportAsync(request);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入用户失败");
                return null;
            }
        }

        #region OpenSpec: optimize-module-list-ui - 状态切换和恢复

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        public async Task<UserDto?> ToggleStatusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("切换用户状态：{Id}", id);
                var response = await _api.ToggleStatusAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("切换用户状态失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("用户状态已切换为：{Status}", response.Data.Status);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态时发生异常：{Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 恢复已删除的用户
        /// </summary>
        public async Task<UserDto?> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("恢复用户：{Id}", id);
                var response = await _api.RestoreAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("恢复用户失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("用户已恢复：{Id}", id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复用户时发生异常：{Id}", id);
                return null;
            }
        }

        #endregion
    }
}
