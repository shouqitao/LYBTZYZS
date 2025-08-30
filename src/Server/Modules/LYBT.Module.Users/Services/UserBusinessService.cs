using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Module.Users;

namespace LYBT.Module.Users.Services
{
    /// <summary>
    /// 用户业务服务 - UltraThink架构
    /// 职责：业务逻辑，状态管理，密码管理，批量操作
    /// </summary>
    public class UserBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserBusinessService> _logger;
        private readonly UserOptions _options;

        public UserBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserBusinessService> logger,
            IOptions<UserOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                if (user.Status == CommonStatus.Disabled)
                    return ServiceResult<bool>.Failure("用户已经是禁用状态");

                user.Status = CommonStatus.Disabled;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("禁用用户成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"禁用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                if (user.Status == CommonStatus.Enabled)
                    return ServiceResult<bool>.Failure("用户已经是启用状态");

                user.Status = CommonStatus.Enabled;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("启用用户成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"启用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return ServiceResult<int>.Failure("用户ID列表不能为空");

                var validIds = ids.Where(id => id != Guid.Empty).ToList();
                if (!validIds.Any())
                    return ServiceResult<int>.Failure("没有有效的用户ID");

                var affectedRows = await _context.Users
                    .Where(u => validIds.Contains(u.Id) && u.Status != CommonStatus.Disabled)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, CommonStatus.Disabled));

                _logger.LogInformation("批量禁用用户成功，影响行数: {Count}", affectedRows);
                return ServiceResult<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量禁用用户失败");
                return ServiceResult<int>.Failure($"批量禁用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量启用用户
        /// </summary>
        public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return ServiceResult<int>.Failure("用户ID列表不能为空");

                var validIds = ids.Where(id => id != Guid.Empty).ToList();
                if (!validIds.Any())
                    return ServiceResult<int>.Failure("没有有效的用户ID");

                var affectedRows = await _context.Users
                    .Where(u => validIds.Contains(u.Id) && u.Status != CommonStatus.Enabled)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.Status, CommonStatus.Enabled));

                _logger.LogInformation("批量启用用户成功，影响行数: {Count}", affectedRows);
                return ServiceResult<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量启用用户失败");
                return ServiceResult<int>.Failure($"批量启用用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 管理员重置密码
        /// </summary>
        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                if (string.IsNullOrWhiteSpace(newPassword))
                    return ServiceResult<bool>.Failure("新密码不能为空");

                if (newPassword.Length < 6)
                    return ServiceResult<bool>.Failure("密码长度不能少于6位");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 更新密码哈希
                user.PasswordHash = PasswordHelper.Hash(newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("重置用户密码成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码失败: {Id}", id);
                return ServiceResult<bool>.Failure($"重置密码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                if (string.IsNullOrWhiteSpace(oldPassword))
                    return ServiceResult<bool>.Failure("原密码不能为空");

                if (string.IsNullOrWhiteSpace(newPassword))
                    return ServiceResult<bool>.Failure("新密码不能为空");

                if (newPassword.Length < 6)
                    return ServiceResult<bool>.Failure("密码长度不能少于6位");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 验证原密码
                if (!PasswordHelper.Verify(oldPassword, user.PasswordHash))
                    return ServiceResult<bool>.Failure("原密码错误");

                // 更新密码哈希
                user.PasswordHash = PasswordHelper.Hash(newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("用户修改密码成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户修改密码失败: {Id}", id);
                return ServiceResult<bool>.Failure($"修改密码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户修改个人信息
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber)
        {
            try
            {
                if (userId == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                if (string.IsNullOrWhiteSpace(realName))
                    return ServiceResult<bool>.Failure("真实姓名不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 更新个人信息
                user.RealName = realName;
                user.PhoneNumber = phoneNumber;
                user.PinYinCode = CommonHelper.GetPinyinCode(realName);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("用户修改个人信息成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改个人信息失败: {UserId}", userId);
                return ServiceResult<bool>.Failure($"修改个人信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建用户业务逻辑（使用统一变更DTO）
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto)
        {
            try
            {
                // 业务规则验证
                var validationResult = await ValidateUserCreationAsync(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);

                // 检查用户名是否重复
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == dto.Username);
                if (existingUser != null)
                    return ServiceResult<UserDto>.Failure("用户名已存在");

                // 使用事务确保数据一致性
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new Entities.Users.User
                    {
                        Id = Guid.NewGuid(),
                        Username = dto.Username,
                        PasswordHash = PasswordHelper.Hash(dto.Password ?? _options.DefaultUserPassword),
                        RealName = dto.RealName,
                        Role = Enum.TryParse<UserRole>(dto.Role, out var createRole) ? createRole : UserRole.Doctor,
                        PhoneNumber = dto.PhoneNumber,
                        Email = dto.Email,
                        Status = dto.Status,
                        PinYinCode = CommonHelper.GetPinyinCode(dto.RealName ?? dto.Username),
                        CreatedTime = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("创建用户成功: {Username} ({Id})", user.Username, user.Id);

                    var resultDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(resultDto);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: Username {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新用户业务逻辑
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<UserDto>.Failure("用户ID不能为空");

                // 业务规则验证
                var validationResult = await ValidateUserUpdateAsync(id, dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<UserDto>.Failure("用户不存在");

                // 使用事务确保数据一致性
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 更新字段
                    user.RealName = dto.RealName;
                    user.Role = Enum.TryParse<UserRole>(dto.Role, out var updateRole) ? updateRole : user.Role;
                    user.PhoneNumber = dto.PhoneNumber;
                    user.Email = dto.Email;
                    user.Status = dto.Status;
                    user.PinYinCode = CommonHelper.GetPinyinCode(dto.RealName);

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("更新用户成功: {Username} ({Id})", user.Username, user.Id);

                    var resultDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(resultDto);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败: {Id}", id);
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除用户业务逻辑
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("用户ID不能为空");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return ServiceResult<bool>.Failure("用户不存在");

                // 业务规则：检查是否可以删除
                if (user.Role == UserRole.Admin)
                {
                    var adminCount = await _context.Users
                        .CountAsync(u => u.Role == UserRole.Admin && u.Status == CommonStatus.Enabled);
                    if (adminCount <= 1)
                        return ServiceResult<bool>.Failure("至少需要保留一个管理员用户");
                }

                // 软删除 - 设置状态为禁用
                user.Status = CommonStatus.Disabled;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("删除用户成功: {Username} ({Id})", user.Username, user.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证用户创建请求
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateUserCreationAsync(UserCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("用户信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Username))
                return ServiceResult<bool>.Failure("用户名不能为空");

            if (dto.Username.Length < 3 || dto.Username.Length > 50)
                return ServiceResult<bool>.Failure("用户名长度必须在3-50字符之间");

            if (string.IsNullOrWhiteSpace(dto.RealName))
                return ServiceResult<bool>.Failure("真实姓名不能为空");

            // 检查用户名格式（只能包含字母、数字、下划线）
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_]+$"))
                return ServiceResult<bool>.Failure("用户名只能包含字母、数字和下划线");

            await Task.CompletedTask; // 保持异步签名
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证用户更新请求
        /// </summary>
        private async Task<ServiceResult<bool>> ValidateUserUpdateAsync(Guid id, UserUpdateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("用户信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.RealName))
                return ServiceResult<bool>.Failure("真实姓名不能为空");

            await Task.CompletedTask; // 保持异步签名
            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}