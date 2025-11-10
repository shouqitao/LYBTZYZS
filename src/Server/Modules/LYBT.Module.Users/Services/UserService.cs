using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Shared.Utilities.Text;
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
    /// 遵循单一服务原则，符合MVP适度设计原则
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            IUserRepository repository,
            IMapper mapper,
            ILogger<UserService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
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
        private async Task<ServiceResult> CanDeleteUserAsync(Guid userId, UserRole targetRole)
        {
            // 权限检查
            var currentRole = GetCurrentUserRole();
            if (!CanManageUser(currentRole, targetRole))
            {
                return ServiceResult.Failure("您没有权限删除该用户");
            }

            // 最后一个SuperAdmin/Admin保护
            if (targetRole == UserRole.SuperAdmin || targetRole == UserRole.Admin)
            {
                // 使用FindAsync查询符合条件的用户数量（IBaseRepository<T>无带参数的CountAsync）
                var users = await _repository.FindAsync(u => u.Role == targetRole);
                var count = users.Count();
                if (count <= 1)
                {
                    var roleName = targetRole == UserRole.SuperAdmin ? "超级管理员" : "管理员";
                    return ServiceResult.Failure($"不能删除最后一个{roleName}");
                }
            }

            return ServiceResult.Success();
        }

        #endregion

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
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Issue #1909: 权限检查 - 不能创建比自己权限高的角色
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, dto.Role))
                {
                    var roleName = dto.Role == UserRole.SuperAdmin ? "超级管理员" :
                                   dto.Role == UserRole.Admin ? "管理员" : "医生";
                    _logger.LogWarning("用户 {CurrentRole} 尝试创建更高权限的用户: {TargetRole}",
                        currentRole, dto.Role);
                    return ServiceResult<UserDto>.Failure($"您没有权限创建{roleName}账户");
                }

                // 保留用户名检查（Issue #1909: 简化，仅保留系统保留用户名）
                var reservedUsernames = new[] { "admin", "administrator", "root", "system", "superadmin", "sysadmin" };
                if (reservedUsernames.Any(reserved => string.Equals(dto.UserName, reserved, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("尝试创建保留用户名: {UserName}", dto.UserName);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.UserName}' 为系统保留用户名，不可使用");
                }

                // Issue #1262: 检查用户名是否已存在（唯一性验证）
                // 使用IUserRepository特定方法检查用户名是否存在
                var existingUser = await _repository.IsUsernameExistsAsync(dto.UserName);
                if (existingUser)
                {
                    _logger.LogWarning("尝试创建重复的用户名: {UserName}", dto.UserName);
                    return ServiceResult<UserDto>.Failure($"用户名 '{dto.UserName}' 已存在，请使用其他用户名");
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
        /// Issue #1909: 添加三角色权限控制
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // Issue #1909: 权限检查 - 只能更新有权限管理的用户
                var currentRole = GetCurrentUserRole();
                if (!CanManageUser(currentRole, entity.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试更新无权限的用户: {TargetUserId}, {TargetRole}",
                        currentRole, id, entity.Role);
                    return ServiceResult<UserDto>.Failure("您没有权限更新该用户");
                }

                // Issue #1909: 检查角色变更权限
                if (dto.Role != entity.Role && !CanManageUser(currentRole, dto.Role))
                {
                    _logger.LogWarning("用户 {CurrentRole} 尝试将用户角色改为更高权限: {OldRole} -> {NewRole}",
                        currentRole, entity.Role, dto.Role);
                    return ServiceResult<UserDto>.Failure("您没有权限将用户角色修改为该级别");
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
        /// Issue #1909: 添加三角色权限控制和最后一个保护
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                // 获取目标用户
                var targetUser = await _repository.GetByIdAsync(id);
                if (targetUser == null)
                    return ServiceResult.Failure("用户不存在");

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
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败");
                return ServiceResult.Failure("删除用户失败");
            }
        }


        /// <summary>
        /// 批量删除用户（软删除）
        /// Issue #1169: 批量操作
        /// Issue #1909: 更新为三角色保护逻辑
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

                        // Issue #1909: 使用统一的权限检查和保护逻辑
                        var permissionCheck = await CanDeleteUserAsync(userId, user.Role);
                        if (!permissionCheck.IsSuccess)
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(userId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = user.UserName,
                                ErrorMessage = permissionCheck.Message ?? "删除失败"
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

                // 生成或使用提供的密码
                // 优先级：1. request.NewPassword 2. 配置文件中的默认密码 3. 随机生成
                string password = request.NewPassword
                    ?? _configuration["Lybt:DefaultPasswords:NewUserPassword"]
                    ?? PasswordHelper.GenerateTemporaryPassword();

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
        /// 修改个人信息 (Issue #1888)
        /// </summary>
        public async Task<ServiceResult<UserDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
        {
            try
            {
                // 验证输入
                if (dto == null)
                {
                    return ServiceResult<UserDto>.Failure("个人资料信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(dto.RealName))
                {
                    return ServiceResult<UserDto>.Failure("真实姓名不能为空");
                }

                // 获取用户实体
                var entity = await _repository.GetByIdAsync(userId);
                if (entity == null)
                {
                    _logger.LogWarning("尝试修改不存在的用户资料: {UserId}", userId);
                    return ServiceResult<UserDto>.Failure("用户不存在");
                }

                // 更新字段
                entity.RealName = dto.RealName;
                entity.PhoneNumber = dto.PhoneNumber;

                // 保存更改
                var updatedEntity = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<UserDto>(updatedEntity);

                _logger.LogInformation("成功修改用户资料: {UserId}, RealName: {RealName}", userId, dto.RealName);
                return ServiceResult<UserDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人资料失败: {UserId}", userId);
                return ServiceResult<UserDto>.Failure("修改个人资料失败");
            }
        }

        #endregion

        #region 私有辅助方法

        // Issue #1757: GenerateTemporaryPassword已移至PasswordHelper.GenerateTemporaryPassword

        #endregion
    }
}
