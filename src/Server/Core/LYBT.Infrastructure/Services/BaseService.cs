using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using System.Security.Claims;

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
        /// 从HttpContext中提取用户信息（异步版本，适用于中间件集成）
        /// </summary>
        /// <param name="context">HttpContext上下文</param>
        /// <returns>用户信息或null</returns>
        protected virtual async Task<(Guid UserId, bool IsAdmin, string Role)?> ExtractUserInfoAsync(HttpContext? context)
        {
            await Task.CompletedTask; // 占位符，实际为同步操作

            if (context?.User?.Identity?.IsAuthenticated != true)
                return null;

            // 尝试从中间件注入的信息获取
            if (context.Items.TryGetValue("MedicalCaseUserInfo", out var userInfoObj) && userInfoObj is MedicalCaseUserInfo userInfo)
            {
                return (userInfo.UserId, userInfo.IsAdmin, userInfo.Role);
            }

            // 备用方案：直接从Claims获取
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                             ?? context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;

            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                     ?? context.User.FindFirst("role")?.Value
                     ?? "Unknown";

            var isAdmin = role.Contains("Admin", StringComparison.OrdinalIgnoreCase)
                         || role.Contains("Administrator", StringComparison.OrdinalIgnoreCase);

            return (userId, isAdmin, role);
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
    /// 泛型BaseService，提供类型安全的权限验证
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseService<T> : BaseService where T : class
    {
        protected BaseService(ILogger logger) : base(logger)
        {
        }

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

        #region 实体属性反射方法

        /// <summary>
        /// 获取实体ID（需要子类实现具体逻辑）
        /// </summary>
        protected abstract Guid GetEntityId<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// 获取创建用户ID（需要子类实现具体逻辑）
        /// </summary>
        protected abstract Guid GetCreatedUserId<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// 获取创建时间（需要子类实现具体逻辑）
        /// </summary>
        protected abstract DateTime GetCreatedDate<TEntity>(TEntity entity) where TEntity : class;

        #endregion
    }
}

/// <summary>
/// MedicalCase用户权限信息（从中间件传递）
/// </summary>
public class MedicalCaseUserInfo
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool CanEditToday { get; set; }
}