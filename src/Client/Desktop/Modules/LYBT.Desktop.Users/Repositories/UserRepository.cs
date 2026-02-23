using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories
{
    /// <summary>
    /// 用户数据仓储实现 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IUserDataSource _dataSource;
        private readonly IUserApi? _api; // 可选，仅用于批量导入/导出等 Remote 模式特有功能
        private readonly ILogger<UserRepository> _logger;

        /// <summary>
        /// 初始化 UserRepository
        /// </summary>
        /// <param name="dataSource">用户数据源（Local 或 Remote）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="api">可选的 API 接口（仅 Remote 模式下注入，用于高级功能）</param>
        public UserRepository(
            IUserDataSource dataSource,
            ILogger<UserRepository> logger,
            IUserApi? api = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api;
        }

        #region 标准 CRUD 操作

        /// <summary>
        /// 分页查询用户列表（返回轻量级 ListDto）
        /// </summary>
        public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("[REPO] User.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);

                var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword);

                var listDtos = items.Select(e => new UserListDto
                {
                    Id = e.Id,
                    UserName = e.UserName,
                    RealName = e.RealName,
                    PhoneNumber = e.PhoneNumber,
                    Role = e.Role,
                    Status = e.Status,
                    LastLoginTime = e.LastLoginTime,
                    CreatedAt = e.CreatedAt
                }).ToList();

                var result = new PagedResult<UserListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("[REPO] User.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.GetPaged failed - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);
                throw;
            }
        }

        /// <summary>
        /// 根据 ID 获取用户详情（返回完整 DetailDto）
        /// </summary>
        public async Task<UserDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] User.GetById started - Id={Id}", id);

                var dto = await _dataSource.GetByIdAsync(id);
                if (dto == null)
                {
                    _logger.LogWarning("[REPO] User.GetById -> NotFound - Id={Id}", id);
                    return null;
                }

                _logger.LogDebug("[REPO] User.GetById completed - Id={Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.GetById failed - Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新用户
        /// </summary>
        public async Task<UserDetailDto> CreateAsync(UserInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] User.Create started - UserName={UserName}", dto.UserName);

                var result = await _dataSource.CreateAsync(dto);

                _logger.LogInformation("[REPO] User.Create completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.Create failed - UserName={UserName}", dto.UserName);
                throw;
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<UserDetailDto> UpdateAsync(UserInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Id == null || dto.Id == Guid.Empty)
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] User.Update started - Id={Id}", dto.Id);

                var result = await _dataSource.UpdateAsync(dto);

                _logger.LogInformation("[REPO] User.Update completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.Update failed - Id={Id}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除用户（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] User.Delete started - Id={Id}", id);

                var result = await _dataSource.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] User.Delete completed - Id={Id}", id);
                }
                else
                {
                    _logger.LogWarning("[REPO] User.Delete -> Failed - Id={Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.Delete failed - Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索用户（基于关键词，返回 ListDto）
        /// </summary>
        public async Task<List<UserListDto>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[REPO] User.Search started - Keyword={Keyword}", keyword);

                var (items, _) = await _dataSource.GetPagedAsync(1, 100, keyword);

                var listDtos = items.Select(e => new UserListDto
                {
                    Id = e.Id,
                    UserName = e.UserName,
                    RealName = e.RealName,
                    PhoneNumber = e.PhoneNumber,
                    Role = e.Role,
                    Status = e.Status,
                    LastLoginTime = e.LastLoginTime,
                    CreatedAt = e.CreatedAt
                }).ToList();

                _logger.LogDebug("[REPO] User.Search completed - Count={Count}", listDtos.Count);
                return listDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.Search failed - Keyword={Keyword}", keyword);
                throw;
            }
        }

        #endregion

        #region 用户专用方法

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<UserDetailDto> GetByUsernameAsync(string username)
        {
            try
            {
                _logger.LogDebug("[REPO] User.GetByUsername started - Username={Username}", username);

                var dto = await _dataSource.GetByUsernameAsync(username);
                if (dto == null)
                {
                    throw new InvalidOperationException($"用户 {username} 不存在");
                }

                _logger.LogDebug("[REPO] User.GetByUsername completed - Username={Username}", username);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.GetByUsername failed - Username={Username}", username);
                throw;
            }
        }

        /// <summary>
        /// 获取所有医生用户（Desktop端本地筛选实现）
        /// </summary>
        public async Task<List<UserListDto>> GetDoctorsAsync()
        {
            try
            {
                _logger.LogDebug("[REPO] User.GetDoctors started");

                // 获取所有用户（第1页，100条，足够覆盖小诊所全部用户）
                var result = await GetPagedAsync(1, 100, null);

                if (result?.Items == null)
                {
                    _logger.LogWarning("[REPO] User.GetDoctors -> Empty result");
                    return new List<UserListDto>();
                }

                // Desktop端本地筛选：角色=医生 && 状态=启用
                var doctors = result.Items
                    .Where(u => u.Role == UserRole.Doctor && u.Status == CommonStatus.Enabled)
                    .ToList();

                _logger.LogInformation("[REPO] User.GetDoctors completed - Count={Count}", doctors.Count);
                return doctors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.GetDoctors failed");
                return new List<UserListDto>();
            }
        }

        /// <summary>
        /// 修改个人资料 (Issue #1891)
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.ChangeProfile -> NotSupported - 本地模式不支持修改个人资料");
                throw new NotSupportedException("本地模式不支持修改个人资料");
            }

            try
            {
                _logger.LogInformation("[REPO] User.ChangeProfile started - UserId={UserId}", userId);

                var response = await _api.ChangeProfileAsync(userId, dto);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("[REPO] User.ChangeProfile completed - UserId={UserId}", userId);
                    return response.Data;
                }

                var errorMsg = response.Message ?? "修改个人资料失败";
                _logger.LogWarning("[REPO] User.ChangeProfile failed - {Message}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.ChangeProfile failed - UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 修改密码 (Issue #1887-1892)
        /// </summary>
        public async Task<ServiceResult> ChangePasswordAsync(Guid userId, LYBT.Shared.Models.Contracts.Auth.ChangePasswordRequest request)
        {
            if (_api == null)
            {
                // 本地模式：使用 DataSource 的 ChangePasswordAsync
                try
                {
                    _logger.LogInformation("[REPO] User.ChangePassword started (Local) - UserId={UserId}", userId);

                    // 注意：本地模式需要先验证旧密码，这里简化处理
                    // 实际实现可能需要 LocalAuthService 来验证
                    var result = await _dataSource.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

                    if (result)
                    {
                        _logger.LogInformation("[REPO] User.ChangePassword completed (Local) - UserId={UserId}", userId);
                        return ServiceResult.Success();
                    }

                    return ServiceResult.Failure("密码修改失败");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[REPO] User.ChangePassword failed (Local) - UserId={UserId}", userId);
                    return ServiceResult.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
                }
            }

            try
            {
                _logger.LogInformation("[REPO] User.ChangePassword started - UserId={UserId}", userId);

                var response = await _api.ChangePasswordAsync(userId, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] User.ChangePassword completed - UserId={UserId}", userId);
                    return ServiceResult.Success();
                }

                var errorMsg = response.Message ?? "修改密码失败";
                _logger.LogWarning("[REPO] User.ChangePassword failed - {Message}", errorMsg);
                return ServiceResult.Failure(errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.ChangePassword failed - UserId={UserId}", userId);
                return ServiceResult.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("修改密码", ex));
            }
        }

        /// <summary>
        /// 管理员重置用户密码 (Issue #1911)
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(
            Guid userId,
            ResetPasswordRequestDto request)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.ResetPassword -> NotSupported - 本地模式不支持重置密码");
                return ServiceResult<ResetPasswordResponseDto>.Failure("本地模式不支持重置密码");
            }

            try
            {
                _logger.LogDebug("[REPO] User.ResetPassword started - UserId={UserId}", userId);

                var apiResponse = await _api.ResetPasswordAsync(userId, request);

                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("[REPO] User.ResetPassword completed - UserId={UserId}", userId);
                    return ServiceResult<ResetPasswordResponseDto>.Success(apiResponse.Data);
                }
                else
                {
                    _logger.LogWarning("[REPO] User.ResetPassword failed - UserId={UserId}, Message={Message}",
                        userId, apiResponse.Message);
                    return ServiceResult<ResetPasswordResponseDto>.Failure(
                        apiResponse.Message ?? "重置密码失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.ResetPassword failed - UserId={UserId}", userId);
                return ServiceResult<ResetPasswordResponseDto>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("重置密码", ex));
            }
        }

        /// <summary>
        /// 批量导入用户 (Issue #2003 Task 2.10)
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.BatchImport -> NotSupported - 本地模式不支持批量导入");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] User.BatchImport started");
                var response = await _api.BatchImportAsync(request);
                _logger.LogInformation("[REPO] User.BatchImport completed");
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.BatchImport failed");
                return null;
            }
        }

        #endregion

        #region 状态切换、恢复和批量操作

        /// <summary>
        /// 切换用户状态（启用/禁用）
        /// </summary>
        public async Task<UserDetailDto?> ToggleStatusAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] User.ToggleStatus started - Id={Id}", id);

                var result = await _dataSource.ToggleStatusAsync(id);
                if (!result)
                {
                    _logger.LogError("[REPO] User.ToggleStatus failed - Id={Id}", id);
                    return null;
                }

                // 重新获取更新后的数据
                var dto = await _dataSource.GetByIdAsync(id);
                if (dto == null)
                {
                    return null;
                }

                _logger.LogInformation("[REPO] User.ToggleStatus completed - Id={Id}, Status={Status}", id, dto.Status);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.ToggleStatus failed - Id={Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 恢复已删除的用户
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<UserDetailDto?> RestoreAsync(Guid id)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.Restore -> NotSupported - 本地模式不支持恢复用户");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] User.Restore started - Id={Id}", id);
                var response = await _api.RestoreAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("[REPO] User.Restore failed - {Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("[REPO] User.Restore completed - Id={Id}", id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.Restore failed - Id={Id}", id);
                return null;
            }
        }

        /// <summary>
        /// 批量删除用户
        /// 注意：仅 Remote 模式支持批量 API 操作
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                // 本地模式：逐个删除
                _logger.LogInformation("[REPO] User.BatchDelete started (Local) - Count={Count}", ids.Count);
                var successCount = 0;
                var failureCount = 0;

                foreach (var id in ids)
                {
                    var result = await _dataSource.DeleteAsync(id);
                    if (result)
                        successCount++;
                    else
                        failureCount++;
                }

                _logger.LogInformation("[REPO] User.BatchDelete completed (Local) - Success={Success}, Failure={Failure}", successCount, failureCount);
                return new BatchOperationResultDto { SuccessCount = successCount, FailureCount = failureCount };
            }

            try
            {
                _logger.LogInformation("[REPO] User.BatchDelete started - Count={Count}", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDeleteAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("[REPO] User.BatchDelete failed - {Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("[REPO] User.BatchDelete completed - Success={Success}, Failure={Failure}",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.BatchDelete failed");
                return null;
            }
        }

        /// <summary>
        /// 批量启用用户
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.BatchEnable -> NotSupported - 本地模式不支持批量启用");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] User.BatchEnable started - Count={Count}", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchEnableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("[REPO] User.BatchEnable failed - {Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("[REPO] User.BatchEnable completed - Success={Success}, Failure={Failure}",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.BatchEnable failed");
                return null;
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] User.BatchDisable -> NotSupported - 本地模式不支持批量禁用");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] User.BatchDisable started - Count={Count}", ids.Count);
                var request = new BatchDeleteInputDto { Ids = ids };
                var response = await _api.BatchDisableAsync(request);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("[REPO] User.BatchDisable failed - {Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("[REPO] User.BatchDisable completed - Success={Success}, Failure={Failure}",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] User.BatchDisable failed");
                return null;
            }
        }

        #endregion
    }
}
