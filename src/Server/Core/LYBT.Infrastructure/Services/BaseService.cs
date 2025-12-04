using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// BaseService统一权限验证基类
    /// Epic #1612: MedicalCase模块权限优化 - Phase 2 Task 2.2
    /// 提供统一的权限验证逻辑，支持当天可改规则
    /// </summary>
    public abstract class BaseService
    {
        protected readonly ILogger _logger;

        protected BaseService(ILogger logger)
        {
            _logger = logger;
        }

        #region 权限验证核心方法

        /// <summary>
        /// 验证编辑权限
        /// Epic #1612: 实现当天可改规则和管理员权限
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="createdUserId">实体创建用户ID</param>
        /// <param name="createdDate">实体创建时间</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <param name="entityType">实体类型（用于日志）</param>
        /// <returns>权限验证结果和错误信息</returns>
        protected virtual (bool IsAuthorized, string ErrorMessage) ValidateEditPermission(
            Guid entityId,
            Guid currentUserId,
            Guid createdUserId,
            DateTime createdDate,
            bool isAdmin = false,
            string entityType = "实体")
        {
            try
            {
                // 管理员始终有权限
                if (isAdmin)
                {
                    _logger.LogDebug("管理员权限验证通过: EntityType={EntityType}, EntityId={EntityId}, AdminUserId={AdminUserId}",
                        entityType, entityId, currentUserId);
                    return (true, string.Empty);
                }

                // 检查是否为本人创建
                if (createdUserId != currentUserId)
                {
                    _logger.LogWarning("权限验证失败 - 非本人创建: EntityType={EntityType}, EntityId={EntityId}, CreatedUserId={CreatedUserId}, CurrentUserId={CurrentUserId}",
                        entityType, entityId, createdUserId, currentUserId);
                    return (false, $"只能编辑自己创建的{entityType}");
                }

                // 检查当天可改规则
                var today = DateTime.Today;
                var createdToday = createdDate.Date == today;

                if (!createdToday)
                {
                    _logger.LogWarning("权限验证失败 - 非当天创建: EntityType={EntityType}, EntityId={EntityId}, CreatedDate={CreatedDate:yyyy-MM-dd}, Today={Today:yyyy-MM-dd}",
                        entityType, entityId, createdDate.Date, today);
                    return (false, $"只能编辑当天创建的{entityType}（创建日期：{createdDate:yyyy-MM-dd}）");
                }

                _logger.LogDebug("权限验证通过: EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}",
                    entityType, entityId, currentUserId);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "权限验证异常: EntityType={EntityType}, EntityId={EntityId}, CurrentUserId={CurrentUserId}",
                    entityType, entityId, currentUserId);
                return (false, "权限验证过程中发生错误");
            }
        }

        /// <summary>
        /// 从HttpContext中提取用户信息
        /// refactor-authorization-system: 现直接从Claims提取，不再依赖中间件
        /// </summary>
        /// <param name="context">HttpContext上下文</param>
        /// <returns>用户信息或null</returns>
        protected virtual Task<(Guid UserId, bool IsAdmin, string Role)?> ExtractUserInfoAsync(HttpContext? context)
        {
            if (context?.User?.Identity?.IsAuthenticated != true)
                return Task.FromResult<(Guid UserId, bool IsAdmin, string Role)?>(null);

            // 直接从Claims获取用户信息
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                             ?? context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Task.FromResult<(Guid UserId, bool IsAdmin, string Role)?>(null);

            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                     ?? context.User.FindFirst("role")?.Value
                     ?? "Unknown";

            var isAdmin = role.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                         || role.Contains("Administrator", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult<(Guid UserId, bool IsAdmin, string Role)?>((userId, isAdmin, role));
        }

        /// <summary>
        /// 验证删除权限（通常比编辑权限更严格）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <param name="createdUserId">实体创建用户ID</param>
        /// <param name="createdDate">实体创建时间</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <param name="entityType">实体类型</param>
        /// <param name="hasRelatedData">是否有关联数据</param>
        /// <returns>权限验证结果和错误信息</returns>
        protected virtual (bool IsAuthorized, string ErrorMessage) ValidateDeletePermission(
            Guid entityId,
            Guid currentUserId,
            Guid createdUserId,
            DateTime createdDate,
            bool isAdmin = false,
            string entityType = "实体",
            bool hasRelatedData = false)
        {
            try
            {
                // 管理员始终有权限
                if (isAdmin)
                {
                    _logger.LogDebug("管理员删除权限验证通过: EntityType={EntityType}, EntityId={EntityId}",
                        entityType, entityId);
                    return (true, string.Empty);
                }

                // 检查是否为本人创建
                if (createdUserId != currentUserId)
                {
                    _logger.LogWarning("删除权限验证失败 - 非本人创建: EntityType={EntityType}, EntityId={EntityId}",
                        entityType, entityId);
                    return (false, $"只能删除自己创建的{entityType}");
                }

                // 检查当天可改规则
                var today = DateTime.Today;
                var createdToday = createdDate.Date == today;

                if (!createdToday)
                {
                    _logger.LogWarning("删除权限验证失败 - 非当天创建: EntityType={EntityType}, EntityId={EntityId}",
                        entityType, entityId);
                    return (false, $"只能删除当天创建的{entityType}");
                }

                // 检查是否有关联数据（删除限制）
                if (hasRelatedData)
                {
                    _logger.LogWarning("删除权限验证失败 - 存在关联数据: EntityType={EntityType}, EntityId={EntityId}",
                        entityType, entityId);
                    return (false, $"存在关联数据，无法删除{entityType}");
                }

                _logger.LogDebug("删除权限验证通过: EntityType={EntityType}, EntityId={EntityId}",
                    entityType, entityId);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除权限验证异常: EntityType={EntityType}, EntityId={EntityId}",
                    entityType, entityId);
                return (false, "删除权限验证过程中发生错误");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查日期是否为今天
        /// </summary>
        protected static bool IsToday(DateTime date)
        {
            return date.Date == DateTime.Today;
        }

        /// <summary>
        /// 获取用户角色的显示名称
        /// </summary>
        protected static string GetRoleDisplayName(string role)
        {
            return role switch
            {
                var r when r.Contains("Admin", StringComparison.OrdinalIgnoreCase) => "管理员",
                var r when r.Contains("Doctor", StringComparison.OrdinalIgnoreCase) => "医生",
                var r when r.Contains("User", StringComparison.OrdinalIgnoreCase) => "用户",
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 记录权限验证日志
        /// </summary>
        protected void LogPermissionValidation(
            string operation,
            string entityType,
            Guid entityId,
            Guid userId,
            bool isAuthorized,
            string? errorMessage = null)
        {
            if (isAuthorized)
            {
                _logger.LogInformation("权限验证通过: Operation={Operation}, EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}",
                    operation, entityType, entityId, userId);
            }
            else
            {
                _logger.LogWarning("权限验证失败: Operation={Operation}, EntityType={EntityType}, EntityId={EntityId}, UserId={UserId}, ErrorMessage={ErrorMessage}",
                    operation, entityType, entityId, userId, errorMessage);
            }
        }

        #endregion
    }

    /// <summary>
    /// 泛型BaseService，提供类型安全的权限验证和统一错误处理
    /// Phase 2: 扩展支持 ExecuteAsync 和 ValidateAsync
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseService<T> : BaseService where T : class
    {
        protected readonly IMapper _mapper;

        protected BaseService(ILogger logger, IMapper mapper) : base(logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region 统一错误处理方法（Phase 2）

        /// <summary>
        /// 执行操作并统一处理异常
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="operation">异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>统一的Result结果</returns>
        protected async Task<Result<TResult>> ExecuteAsync<TResult>(
            Func<Task<TResult>> operation,
            string operationName)
        {
            try
            {
                var result = await operation();
                return Result<TResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Operation} 失败", operationName);
                return Result<TResult>.Failure($"{operationName}失败");
            }
        }

        /// <summary>
        /// 执行无返回值操作并统一处理异常
        /// </summary>
        /// <param name="operation">异步操作</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>统一的Result结果</returns>
        protected async Task<Result> ExecuteAsync(
            Func<Task> operation,
            string operationName)
        {
            try
            {
                await operation();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Operation} 失败", operationName);
                return Result.Failure($"{operationName}失败");
            }
        }

        /// <summary>
        /// 执行验证
        /// </summary>
        /// <typeparam name="TDto">DTO类型</typeparam>
        /// <param name="dto">待验证的DTO</param>
        /// <param name="validator">验证器</param>
        /// <returns>验证结果</returns>
        protected async Task<Result<TDto>> ValidateAsync<TDto>(
            TDto dto,
            IValidator<TDto> validator)
        {
            var validationResult = await validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Result<TDto>.Failure(errors);
            }
            return Result<TDto>.Success(dto);
        }

        /// <summary>
        /// 同步验证（适用于简单场景）
        /// </summary>
        protected Result<TDto> Validate<TDto>(
            TDto dto,
            IValidator<TDto> validator)
        {
            var validationResult = validator.Validate(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Result<TDto>.Failure(errors);
            }
            return Result<TDto>.Success(dto);
        }

        #endregion

        /// <summary>
        /// 验证实体编辑权限（泛型版本）
        /// </summary>
        protected virtual (bool IsAuthorized, string ErrorMessage) ValidateEditPermission<TEntity>(
            TEntity entity,
            Guid currentUserId,
            bool isAdmin = false) where TEntity : class
        {
            // 使用反射获取实体的关键属性
            var entityType = typeof(TEntity).Name;
            var entityId = GetEntityId(entity);
            var createdUserId = GetCreatedUserId(entity);
            var createdDate = GetCreatedDate(entity);

            return ValidateEditPermission(entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType);
        }

        #region 实体属性反射方法（虚方法，需要权限验证功能的子类需重写）

        /// <summary>
        /// 获取实体ID（需要权限验证的子类需重写此方法）
        /// </summary>
        protected virtual Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class
        {
            throw new NotImplementedException($"子类 {GetType().Name} 需要重写 GetEntityId 方法以支持权限验证");
        }

        /// <summary>
        /// 获取创建用户ID（需要权限验证的子类需重写此方法）
        /// </summary>
        protected virtual Guid GetCreatedUserId<TEntity>(TEntity entity) where TEntity : class
        {
            throw new NotImplementedException($"子类 {GetType().Name} 需要重写 GetCreatedUserId 方法以支持权限验证");
        }

        /// <summary>
        /// 获取创建时间（需要权限验证的子类需重写此方法）
        /// </summary>
        protected virtual DateTime GetCreatedDate<TEntity>(TEntity entity) where TEntity : class
        {
            throw new NotImplementedException($"子类 {GetType().Name} 需要重写 GetCreatedDate 方法以支持权限验证");
        }

        #endregion
    }
}
// refactor-authorization-system: MedicalCaseUserInfo 已删除，权限现通过 IAuthorizationService 处理
