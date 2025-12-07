using System.Security.Claims;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;

namespace LYBT.WebAPI.Authorization;

/// <summary>
/// 验方资源授权处理器
/// optimize-api-permissions: 实现资源级授权检查
///
/// 授权规则:
/// - Admin/SuperAdmin: 可以操作所有Formula
/// - Doctor:
///   - Read: 可以读取自己创建的Formula（Admin创建的通过Service层过滤）
///   - Update/Delete: 只能操作自己创建的Formula
/// </summary>
public class FormulaAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Formula>
{
    private readonly ILogger<FormulaAuthorizationHandler> _logger;

    public FormulaAuthorizationHandler(ILogger<FormulaAuthorizationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Formula resource)
    {
        var (userId, role) = ExtractUserInfo(context.User);

        if (userId == Guid.Empty)
        {
            _logger.LogWarning(
                "授权失败: 无法从 Claims 提取用户ID, Operation={Operation}, ResourceId={ResourceId}",
                requirement.Name, resource.Id);
            return Task.CompletedTask;
        }

        bool authorized = EvaluateAuthorization(userId, role, requirement, resource);

        if (authorized)
        {
            _logger.LogDebug(
                "授权成功: UserId={UserId}, Role={Role}, Operation={Operation}, ResourceId={ResourceId}",
                userId, role, requirement.Name, resource.Id);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "授权失败: UserId={UserId}, Role={Role}, Operation={Operation}, ResourceId={ResourceId}, " +
                "FormulaUserId={FormulaUserId}",
                userId, role, requirement.Name, resource.Id, resource.UserId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 评估授权逻辑
    /// </summary>
    private bool EvaluateAuthorization(
        Guid userId,
        UserRole role,
        OperationAuthorizationRequirement requirement,
        Formula resource)
    {
        // Admin/SuperAdmin可以操作所有Formula
        if (role is UserRole.SuperAdmin or UserRole.Admin)
        {
            return true;
        }

        // Doctor的授权逻辑
        return requirement.Name switch
        {
            nameof(FormulaOperations.Read) =>
                // Doctor可以读取自己创建的Formula
                // 注意: Admin创建的Formula通过Service层GetPagedAsync过滤提供
                resource.UserId == userId,

            nameof(FormulaOperations.Update) or nameof(FormulaOperations.Delete) =>
                // Doctor只能修改/删除自己创建的Formula
                resource.UserId == userId,

            _ => false
        };
    }

    /// <summary>
    /// 从 ClaimsPrincipal 提取用户ID和角色
    /// </summary>
    private static (Guid UserId, UserRole Role) ExtractUserInfo(ClaimsPrincipal user)
    {
        var userId = Guid.Empty;
        var role = UserRole.Doctor; // 默认最低权限

        // 提取用户ID: 优先 NameIdentifier, 其次 sub
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                          ?? user.FindFirst("sub");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        // 提取角色: 优先 Role claim, 其次 role claim
        var roleClaim = user.FindFirst(ClaimTypes.Role)
                        ?? user.FindFirst("role");
        if (roleClaim != null)
        {
            // 处理遗留命名 (SysAdmin → SuperAdmin)
            var roleValue = roleClaim.Value;
            if (roleValue.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            {
                role = UserRole.SuperAdmin;
            }
            else if (Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out var parsedRole))
            {
                role = parsedRole;
            }
        }

        return (userId, role);
    }
}
