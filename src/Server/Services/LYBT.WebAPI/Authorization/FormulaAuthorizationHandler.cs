using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

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
        var (userId, role) = context.User.ExtractUserInfo();

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

    // DRY: ExtractUserInfo 已提取到 ClaimsPrincipalExtensions 扩展方法
}
