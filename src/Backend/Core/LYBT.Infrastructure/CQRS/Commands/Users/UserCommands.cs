using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.CQRS.Commands;
using LYBT.Infrastructure.Repositories;
using LYBT.Models;
using LYBT.Models.Users;
using LYBT.Shared.Interfaces.Caching;
using LYBT.Domain.Aggregates.UserAggregate.ValueObjects;

namespace LYBT.Infrastructure.CQRS.Commands.Users
{
    #region Command Definitions

    /// <summary>
    /// 创建用户命令
    /// </summary>
    public record CreateUserCommand : CommandBase<UserModel>
    {
        public string UserName { get; init; }
        public string RealName { get; init; }
        public string Email { get; init; }
        public string PhoneNumber { get; init; }
        public UserRole Role { get; init; }
        public string PasswordHash { get; init; }
        public bool IsActive { get; init; } = true;

        public CreateUserCommand(string userName, string realName, string email, UserRole role, string passwordHash)
        {
            UserName = userName;
            RealName = realName;
            Email = email;
            Role = role;
            PasswordHash = passwordHash;
        }
    }

    /// <summary>
    /// 更新用户命令
    /// </summary>
    public record UpdateUserCommand : CommandBase<UserModel>
    {
        public Guid Id { get; init; }
        public string RealName { get; init; }
        public string Email { get; init; }
        public string PhoneNumber { get; init; }
        public UserRole? Role { get; init; }
        public bool? IsActive { get; init; }

        public UpdateUserCommand(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// 删除用户命令
    /// </summary>
    public record DeleteUserCommand : CommandBase<bool>
    {
        public Guid Id { get; init; }

        public DeleteUserCommand(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// 更新用户密码命令
    /// </summary>
    public record UpdateUserPasswordCommand : CommandBase<bool>
    {
        public Guid Id { get; init; }
        public string NewPasswordHash { get; init; }

        public UpdateUserPasswordCommand(Guid id, string newPasswordHash)
        {
            Id = id;
            NewPasswordHash = newPasswordHash;
        }
    }

    /// <summary>
    /// 更新用户最后登录时间命令
    /// </summary>
    public record UpdateUserLastLoginCommand : CommandBase<bool>
    {
        public Guid Id { get; init; }
        public DateTime LoginTime { get; init; }

        public UpdateUserLastLoginCommand(Guid id, DateTime loginTime)
        {
            Id = id;
            LoginTime = loginTime;
        }
    }

    /// <summary>
    /// 批量删除用户命令
    /// </summary>
    public record BatchDeleteUsersCommand : CommandBase<int>
    {
        public Guid[] UserIds { get; init; }

        public BatchDeleteUsersCommand(params Guid[] userIds)
        {
            UserIds = userIds ?? Array.Empty<Guid>();
        }
    }

    #endregion

    #region Command Handlers

    /// <summary>
    /// 创建用户命令处理器
    /// </summary>
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserModel>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<CreateUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserModel> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始执行创建用户命令: {UserName}", request.UserName);

            try
            {
                // 检查用户名和邮箱是否已存在
                var exists = await _userRepository.ExistsAsync(request.UserName, request.Email);
                if (exists)
                {
                    _logger.LogWarning("用户名或邮箱已存在: {UserName}, {Email}", request.UserName, request.Email);
                    throw new InvalidOperationException($"用户名 '{request.UserName}' 或邮箱 '{request.Email}' 已存在");
                }

                // 创建用户实体
                var user = new UserModel
                {
                    Id = Guid.NewGuid(),
                    UserName = request.UserName,
                    RealName = request.RealName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Role = request.Role,
                    PasswordHash = request.PasswordHash,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 保存到数据库
                await _userRepository.AddAsync(user);
                var savedCount = await _userRepository.SaveChangesAsync();

                if (savedCount > 0)
                {
                    // 清除相关缓存
                    await InvalidateUserCaches(user.Id, user.UserName);
                    
                    _logger.LogInformation("用户创建成功: {UserId}, {UserName}", user.Id, user.UserName);
                    return user;
                }

                throw new InvalidOperationException("保存用户失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: {UserName}", request.UserName);
                throw;
            }
        }

        private async Task InvalidateUserCaches(Guid userId, string userName)
        {
            try
            {
                // 清除用户相关的缓存
                await _cacheService.RemoveAsync($"user:id:{userId}");
                await _cacheService.RemoveAsync($"user:username:{userName}");
                
                // 清除列表和统计缓存 - 可以考虑使用更智能的缓存失效策略
                await _cacheService.RemoveAsync("user:statistics:start:all:end:all");
                
                _logger.LogDebug("用户缓存已失效: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除用户缓存时发生错误: {UserId}", userId);
            }
        }
    }

    /// <summary>
    /// 更新用户命令处理器
    /// </summary>
    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserModel>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<UpdateUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserModel> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始执行更新用户命令: {UserId}", request.Id);

            try
            {
                // 获取现有用户
                var existingUser = await _userRepository.GetByIdAsync(request.Id);
                if (existingUser == null)
                {
                    throw new InvalidOperationException($"用户不存在: {request.Id}");
                }

                // 更新字段
                if (!string.IsNullOrEmpty(request.RealName))
                    existingUser.RealName = request.RealName;

                if (!string.IsNullOrEmpty(request.Email))
                    existingUser.Email = request.Email;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    existingUser.PhoneNumber = request.PhoneNumber;

                if (request.Role.HasValue)
                    existingUser.Role = request.Role.Value;

                if (request.IsActive.HasValue)
                    existingUser.IsActive = request.IsActive.Value;

                existingUser.UpdatedAt = DateTime.Now;

                // 保存更改
                await _userRepository.UpdateAsync(existingUser);
                var savedCount = await _userRepository.SaveChangesAsync();

                if (savedCount > 0)
                {
                    // 清除相关缓存
                    await InvalidateUserCaches(existingUser.Id, existingUser.UserName);
                    
                    _logger.LogInformation("用户更新成功: {UserId}", request.Id);
                    return existingUser;
                }

                throw new InvalidOperationException("保存用户更新失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户失败: {UserId}", request.Id);
                throw;
            }
        }

        private async Task InvalidateUserCaches(Guid userId, string userName)
        {
            try
            {
                await _cacheService.RemoveAsync($"user:id:{userId}");
                await _cacheService.RemoveAsync($"user:username:{userName}");
                await _cacheService.RemoveAsync("user:statistics:start:all:end:all");
                
                _logger.LogDebug("用户缓存已失效: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除用户缓存时发生错误: {UserId}", userId);
            }
        }
    }

    /// <summary>
    /// 删除用户命令处理器
    /// </summary>
    public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始执行删除用户命令: {UserId}", request.Id);

            try
            {
                // 获取用户信息用于缓存失效
                var user = await _userRepository.GetByIdAsync(request.Id);
                if (user == null)
                {
                    _logger.LogWarning("尝试删除不存在的用户: {UserId}", request.Id);
                    return false;
                }

                // 执行删除
                var deleted = await _userRepository.DeleteAsync(request.Id);
                if (deleted)
                {
                    var savedCount = await _userRepository.SaveChangesAsync();
                    if (savedCount > 0)
                    {
                        // 清除相关缓存
                        await InvalidateUserCaches(user.Id, user.UserName);
                        
                        _logger.LogInformation("用户删除成功: {UserId}", request.Id);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {UserId}", request.Id);
                throw;
            }
        }

        private async Task InvalidateUserCaches(Guid userId, string userName)
        {
            try
            {
                await _cacheService.RemoveAsync($"user:id:{userId}");
                await _cacheService.RemoveAsync($"user:username:{userName}");
                await _cacheService.RemoveAsync("user:statistics:start:all:end:all");
                
                _logger.LogDebug("用户缓存已失效: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除用户缓存时发生错误: {UserId}", userId);
            }
        }
    }

    /// <summary>
    /// 更新用户密码命令处理器
    /// </summary>
    public class UpdateUserPasswordCommandHandler : ICommandHandler<UpdateUserPasswordCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserPasswordCommandHandler> _logger;

        public UpdateUserPasswordCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserPasswordCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始执行更新用户密码命令: {UserId}", request.Id);

            try
            {
                var success = await _userRepository.UpdatePasswordAsync(request.Id, request.NewPasswordHash);
                
                if (success)
                {
                    _logger.LogInformation("用户密码更新成功: {UserId}", request.Id);
                }
                else
                {
                    _logger.LogWarning("用户密码更新失败，用户可能不存在: {UserId}", request.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户密码失败: {UserId}", request.Id);
                throw;
            }
        }
    }

    /// <summary>
    /// 更新用户最后登录时间命令处理器
    /// </summary>
    public class UpdateUserLastLoginCommandHandler : ICommandHandler<UpdateUserLastLoginCommand, bool>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UpdateUserLastLoginCommandHandler> _logger;

        public UpdateUserLastLoginCommandHandler(
            IUserRepository userRepository,
            ILogger<UpdateUserLastLoginCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(UpdateUserLastLoginCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("开始执行更新用户最后登录时间命令: {UserId}", request.Id);

            try
            {
                var success = await _userRepository.UpdateLastLoginAsync(request.Id, request.LoginTime);
                
                if (success)
                {
                    _logger.LogDebug("用户最后登录时间更新成功: {UserId}", request.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户最后登录时间失败: {UserId}", request.Id);
                throw;
            }
        }
    }

    /// <summary>
    /// 批量删除用户命令处理器
    /// </summary>
    public class BatchDeleteUsersCommandHandler : ICommandHandler<BatchDeleteUsersCommand, int>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<BatchDeleteUsersCommandHandler> _logger;

        public BatchDeleteUsersCommandHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<BatchDeleteUsersCommandHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> Handle(BatchDeleteUsersCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始执行批量删除用户命令，数量: {Count}", request.UserIds.Length);

            if (request.UserIds.Length == 0)
            {
                return 0;
            }

            try
            {
                await _userRepository.BeginTransactionAsync();
                
                var deletedCount = 0;
                
                // 获取要删除的用户信息（用于缓存失效）
                var usersToDelete = new System.Collections.Generic.List<UserModel>();
                foreach (var userId in request.UserIds)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        usersToDelete.Add(user);
                    }
                }

                // 执行批量删除
                foreach (var userId in request.UserIds)
                {
                    var deleted = await _userRepository.DeleteAsync(userId);
                    if (deleted)
                    {
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    var savedCount = await _userRepository.SaveChangesAsync();
                    if (savedCount > 0)
                    {
                        await _userRepository.CommitTransactionAsync();
                        
                        // 清除相关缓存
                        foreach (var user in usersToDelete)
                        {
                            await InvalidateUserCache(user.Id, user.UserName);
                        }
                        
                        _logger.LogInformation("批量删除用户成功，删除数量: {DeletedCount}", deletedCount);
                        return deletedCount;
                    }
                }

                await _userRepository.RollbackTransactionAsync();
                return 0;
            }
            catch (Exception ex)
            {
                await _userRepository.RollbackTransactionAsync();
                _logger.LogError(ex, "批量删除用户失败");
                throw;
            }
        }

        private async Task InvalidateUserCache(Guid userId, string userName)
        {
            try
            {
                await _cacheService.RemoveAsync($"user:id:{userId}");
                await _cacheService.RemoveAsync($"user:username:{userName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清除用户缓存时发生错误: {UserId}", userId);
            }
        }
    }

    #endregion
}