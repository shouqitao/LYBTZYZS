using System;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Options;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Module.Users.Services.Core
{
    /// <summary>
    /// 用户基础CRUD操作服务实现
    /// UltraThink重构：单一职责原则，只负责用户的基础增删改查操作
    /// 代码行数：约150行，符合500行以下标准
    /// </summary>
    public class UserCrudService(
        AppDbContext context,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserCrudService> logger,
        IOptions<UserOptions> options,
        UserValidationHelper validationHelper) : IUserCrudService
    {
        private readonly AppDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly ILogger<UserCrudService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly UserOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly UserValidationHelper _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));

        /// <summary>
        /// 创建用户并自动处理业务逻辑
        /// </summary>
        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto)
        {
            try
            {
                // 验证创建请求 - 临时使用内联验证，稍后Helper会被更新
                var validation = ValidateMutationDto(dto, true); // true for create
                if (!validation.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validation.ErrorMessage!);

                // 使用ExecutionStrategy处理事务以兼容重试策略
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var user = CreateUserFromDto(dto);
                        
                        // 添加到DbSet但不保存，让事务统一处理保存
                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();

                        if (user != null)
                        {
                            // 记录操作日志
                            await LogUserOperation(
                                user.Id, ActionType.Create, Guid.Empty, "System",
                                $"新增用户：{user.Username}",
                                newValue: user
                            );

                            await transaction.CommitAsync();
                            var userDto = _mapper.Map<UserDto>(user);
                            _logger.LogInformation("创建用户成功: {Username} (ID: {UserId})", user.Username, user.Id);
                            return ServiceResult<UserDto>.Success(userDto);
                        }

                        await transaction.RollbackAsync();
                        return ServiceResult<UserDto>.Failure("用户创建失败");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败, Username: {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure($"创建用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新用户信息并处理业务逻辑
        /// </summary>
        public async Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto)
        {
            try
            {
                // 验证更新请求 - 临时使用内联验证，稍后Helper会被更新
                var validation = ValidateMutationDto(dto, false); // false for update
                if (!validation.IsSuccess)
                    return ServiceResult<UserDto>.Failure(validation.ErrorMessage!);

                var existingUser = await GetExistingUser(id);
                var oldSnapshot = JsonSerializer.Serialize(existingUser);

                // 使用ExecutionStrategy处理事务以兼容重试策略
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // 手动更新字段而不是使用AutoMapper，因为DTO结构改变了
                        existingUser.RealName = dto.RealName;
                        existingUser.Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Doctor;
                        existingUser.PhoneNumber = dto.PhoneNumber;
                        existingUser.Email = dto.Email;
                        existingUser.Status = dto.Status;
                        existingUser.PinYinCode = CommonHelper.GetPinyinCode(dto.RealName ?? existingUser.Username);
                        
                        // 更新实体但不保存，让事务统一处理保存
                        _context.Users.Update(existingUser);
                        await _context.SaveChangesAsync();

                        if (existingUser != null)
                        {
                            await LogUserOperation(
                                existingUser.Id, ActionType.Update, Guid.Empty, "System",
                                $"修改用户信息：{existingUser.Username}",
                                oldValue: oldSnapshot, newValue: JsonSerializer.Serialize(existingUser)
                            );

                            await transaction.CommitAsync();
                            var userDto = _mapper.Map<UserDto>(existingUser);
                            _logger.LogInformation("更新用户成功: {Username} (ID: {UserId})", existingUser.Username, id);
                            return ServiceResult<UserDto>.Success(userDto);
                        }

                        await transaction.RollbackAsync();
                        return ServiceResult<UserDto>.Failure("用户更新失败");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败, ID: {UserId}", id);
                return ServiceResult<UserDto>.Failure($"更新用户失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteUserAsync(Guid id)
        {
            try
            {
                var user = await GetExistingUser(id);
                var result = await _userRepository.DeleteAsync(id);

                if (result)
                {
                    await LogUserOperation(
                        id, ActionType.Delete, Guid.Empty, "System",                        $"删除用户：{user.Username}",                        oldValue: JsonSerializer.Serialize(user)
                    );

                    _logger.LogInformation("删除用户成功: {Username} (ID: {UserId})", user.Username, id);                }

                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败, ID: {UserId}", id);                return ServiceResult<bool>.Failure($"删除用户失败: {ex.Message}", ex);            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 从DTO创建用户实体
        /// </summary>
        private User CreateUserFromDto(UserMutationDto dto)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                RealName = dto.RealName,
                Role = Enum.TryParse<UserRole>(dto.Role, out var role) ? role : UserRole.Doctor,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Status = dto.Status,
                CreatedTime = DateTime.Now,
                PasswordHash = PasswordHelper.Hash(dto.Password ?? _options.DefaultUserPassword),
                PinYinCode = CommonHelper.GetPinyinCode(dto.RealName ?? dto.Username)
            };
            
            return user;
        }

        /// <summary>
        /// 获取已存在的用户
        /// </summary>
        private async Task<User> GetExistingUser(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new InvalidOperationException($"用户不存在: {id}");            
            return user;
        }

        /// <summary>
        /// 记录用户操作日志
        /// </summary>
        private Task LogUserOperation(
            Guid targetUserId, ActionType actionType, Guid operatorId, string operatorName, 
            string description, object? oldValue = null, object? newValue = null)
        {
            try
            {
                // TODO: 实现操作日志记录
                _logger.LogInformation(
                    "用户操作日志 - 目标用户: {TargetUserId}, 操作类型: {ActionType}, 操作者: {OperatorName}, 描述: {Description}",                    targetUserId, actionType, operatorName, description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录用户操作日志失败");
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 验证用户变更DTO - 临时内联验证（后续Helper更新后会统一）
        /// </summary>
        private static ServiceResult<bool> ValidateMutationDto(UserMutationDto dto, bool isCreateOperation)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("用户信息不能为空");

            // 创建操作的额外验证
            if (isCreateOperation)
            {
                if (string.IsNullOrWhiteSpace(dto.Username))
                    return ServiceResult<bool>.Failure("用户名不能为空");

                if (dto.Username.Length < 3 || dto.Username.Length > 50)
                    return ServiceResult<bool>.Failure("用户名长度必须在3-50字符之间");
            }

            // 通用验证（创建和更新都需要）
            if (string.IsNullOrWhiteSpace(dto.RealName))
                return ServiceResult<bool>.Failure("真实姓名不能为空");

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}


