using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Utilities;
using LYBT.Module.Users.Interfaces;
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
        /// 分页获取用户列表（Issue #1162: 扩展支持角色和状态筛选）
        /// </summary>
        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dtos = _mapper.Map<List<UserDto>>(pagedResult.Items);

                // 应用筛选条件（MVP阶段内存过滤）
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    dtos = dtos.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.RealName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (u.Email != null && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Issue #1162: 按角色筛选
                if (role.HasValue)
                {
                    dtos = dtos.Where(u => u.Role == role.Value).ToList();
                }

                // Issue #1162: 按状态筛选
                if (status.HasValue)
                {
                    dtos = dtos.Where(u => u.Status == status.Value).ToList();
                }

                var result = new PagedResult<UserDto>
                {
                    Items = dtos,
                    TotalCount = keyword == null && !role.HasValue && !status.HasValue
                        ? pagedResult.TotalCount
                        : dtos.Count,
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
        public async Task<ServiceResult<UserDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 获取超级管理员用户名（可配置）
                var sysAdminUsername = _configuration["Lybt:Business:SystemAdmin:UserName"] ?? "clinic_admin";

                // 检查是否尝试使用超级管理员用户名
                if (string.Equals(dto.UserName, sysAdminUsername, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("尝试创建与超级管理员相同的用户名: {UserName}", dto.UserName);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
                }

                // 可选：添加其他保留用户名列表
                var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
                if (reservedUsernames.Any(reserved => string.Equals(dto.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("尝试创建保留用户名: {UserName}", dto.UserName);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
                }

                // Issue #1262: 检查用户名是否已存在（唯一性验证）
                var existingUser = await _repository.ExistsAsync(u => u.UserName == dto.UserName);
                if (existingUser)
                {
                    _logger.LogWarning("尝试创建重复的用户名: {UserName}", dto.UserName);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.UserName}' 已存在，请使用其他用户名");
                }

                var entity = _mapper.Map<User>(dto);

                // Issue #1262: 对密码进行哈希处理，如果未提供密码则使用默认密码
                string passwordToHash;
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    passwordToHash = dto.Password;
                    _logger.LogDebug("使用用户提供的密码创建用户: {UserName}", dto.UserName);
                }
                else
                {
                    // 从配置读取默认密码：Lybt:Authentication:DefaultPasswords:NewUserPassword
                    passwordToHash = _configuration["Lybt:Authentication:DefaultPasswords:NewUserPassword"] ?? "Lybt2025@TempPass!";
                    _logger.LogInformation("使用系统默认密码创建用户: {UserName}，密码配置: Lybt:Authentication:DefaultPasswords:NewUserPassword", dto.UserName);
                }

                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<UserDto>(result);

                _logger.LogInformation("成功创建用户: {UserName}, Role: {Role}", resultDto.UserName, resultDto.Role);
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
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // 注意：UserInputDto不包含Username属性，用户名一旦创建不可更改
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
        /// 批量删除用户（软删除）(Issue #1169)
        /// </summary>
        public async Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            const int MAX_BATCH_SIZE = 100;

            try
            {
                // 批量大小限制
                if (ids.Count > MAX_BATCH_SIZE)
                {
                    return ServiceResult<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
                }

                var result = new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    IsSuccess = true,
                    Message = "批量删除完成"
                };

                // 获取超级管理员用户名（可配置）
                var sysAdminUsername = _configuration["Lybt:Business:SystemAdmin:UserName"] ?? "clinic_admin";

                foreach (var userId in ids)
                {
                    try
                    {
                        // 检查用户是否存在
                        var user = await _repository.GetByIdAsync(userId);
                        if (user == null)
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(userId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = userId.ToString(),
                                ErrorMessage = "用户不存在"
                            });
                            continue;
                        }

                        // 检查是否是超级管理员
                        if (user.Role == UserRole.Admin || 
                            string.Equals(user.UserName, sysAdminUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(userId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = user.UserName,
                                ErrorMessage = "不能删除超级管理员"
                            });
                            continue;
                        }

                        // 执行删除
                        var deleteResult = await _repository.DeleteAsync(userId);
                        if (deleteResult)
                        {
                            result.SuccessCount++;
                            result.SuccessfulIds.Add(userId);
                        }
                        else
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(userId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = userId.ToString(),
                                ErrorMessage = "删除失败"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(userId);
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = userId.ToString(),
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "批量删除用户失败: {UserId}", userId);
                    }
                }

                // 更新操作结果
                result.IsSuccess = result.FailureCount == 0;
                if (result.FailureCount > 0 && result.SuccessCount > 0)
                {
                    result.Message = $"部分成功：成功{result.SuccessCount}条，失败{result.FailureCount}条";
                }
                else if (result.FailureCount == result.TotalCount)
                {
                    result.Message = "批量删除失败";
                    result.IsSuccess = false;
                }

                _logger.LogInformation("批量删除用户完成: 总数{Total}, 成功{Success}, 失败{Failed}", 
                    result.TotalCount, result.SuccessCount, result.FailureCount);

                return ServiceResult<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除用户异常");
                return ServiceResult<BatchOperationResultDto>.Failure("批量删除用户失败");
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
        /// 切换用户状态 (Issue #1162)
        /// </summary>
        public async Task<ServiceResult<UserDto>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // 切换状态
                entity.Status = entity.Status == CommonStatus.Enabled
                    ? CommonStatus.Disabled
                    : CommonStatus.Enabled;

                var updatedEntity = await _repository.UpdateAsync(entity);
                var dto = _mapper.Map<UserDto>(updatedEntity);

                _logger.LogInformation("切换用户状态成功: {UserId}, 新状态: {Status}", id, entity.Status);
                return ServiceResult<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态失败");
                return ServiceResult<UserDto>.Failure("切换用户状态失败");
            }
        }

        /// <summary>
        /// 管理员重置密码（Issue #1162: 支持自动生成临时密码）
        /// </summary>
        public async Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ResetPasswordResponseDto>.Failure("用户不存在");

                // 生成或使用提供的密码（Issue #1757: 使用PasswordHelper）
                string password = request.NewPassword ?? PasswordHelper.GenerateTemporaryPassword();

                // 哈希密码并更新
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _repository.UpdateAsync(entity);

                var response = new ResetPasswordResponseDto
                {
                    Success = true,
                    TemporaryPassword = string.IsNullOrEmpty(request.NewPassword) ? password : string.Empty
                };

                _logger.LogInformation("重置用户密码成功: {UserId}, 自动生成: {AutoGenerated}",
                    id, string.IsNullOrEmpty(request.NewPassword));

                return ServiceResult<ResetPasswordResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码失败");
                return ServiceResult<ResetPasswordResponseDto>.Failure("重置密码失败");
            }
        }

        /// <summary>
        /// 重置密码（向后兼容方法）
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

        #region 私有辅助方法

        // Issue #1757: GenerateTemporaryPassword已移至PasswordHelper.GenerateTemporaryPassword

        #endregion
    }
}
