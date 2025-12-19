using System.Security.Claims;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;

namespace LYBT.WebAPI.Authorization;

/// <summary>
/// 医案资源授权处理器
/// 实现 ASP.NET Core 资源级授权，委托给 IMedicalCasePermissionService 执行实际权限判断
/// </summary>
public class MedicalCaseAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, MedicalCase>
{
    private readonly IMedicalCasePermissionService _permissionService;
    private readonly ILogger<MedicalCaseAuthorizationHandler> _logger;

    public MedicalCaseAuthorizationHandler(
        IMedicalCasePermissionService permissionService,
        ILogger<MedicalCaseAuthorizationHandler> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        MedicalCase resource)
    {
        var (userId, role) = ExtractUserInfo(context.User);

        if (userId == Guid.Empty)
        {
            _logger.LogWarning(
                "授权失败: 无法从 Claims 提取用户ID, Operation={Operation}, ResourceId={ResourceId}",
                requirement.Name, resource.Id);
            return Task.CompletedTask;
        }

        bool authorized = requirement.Name switch
        {
            nameof(MedicalCaseOperations.Create) =>
                _permissionService.CanCreate(userId, role),
            nameof(MedicalCaseOperations.Edit) =>
                _permissionService.CanEdit(userId, role, resource),
            nameof(MedicalCaseOperations.Delete) =>
                _permissionService.CanDelete(userId, role, resource),
            nameof(MedicalCaseOperations.Read) => true, // 已认证即可读
            _ => false
        };

        if (authorized)
        {
            _logger.LogDebug(
                "授权成功: UserId={UserId}, Role={Role}, Operation={Operation}, ResourceId={ResourceId}",
                userId, role, requirement.Name, resource.Id);
            context.Succeed(requirement);
        }
        else
        {
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            _logger.LogWarning(
                "授权失败: UserId={UserId}, Role={Role}, Operation={Operation}, ResourceId={ResourceId}, " +
                "MedicalCaseUserId={MedicalCaseUserId}, CaseStatus={CaseStatus}",
                userId, role, requirement.Name, resource.Id, resource.UserId, resource.CaseStatus);
        }

        return Task.CompletedTask;
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
