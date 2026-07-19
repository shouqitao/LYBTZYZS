using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 所有权验证过滤器 — 从 ActionArguments 中查找 Guid 类型的 createdBy 参数并验证
/// 使用方式: [ServiceFilter(typeof(OwnershipValidationFilter))]
/// </summary>
public class OwnershipValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var createdByArg = context.ActionArguments.Values
            .OfType<Guid?>()
            .FirstOrDefault();

        if (createdByArg.HasValue)
        {
            var user = context.HttpContext.User;
            var roleStr = user?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var isAdmin = roleStr is "SuperAdmin" or "Admin";

            if (!isAdmin)
            {
                var userIdClaim = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var operatorId) || createdByArg.Value != operatorId)
                {
                    var controller = context.Controller as ControllerBase;
                    if (controller != null)
                    {
                        context.Result = controller.StatusCode(403, new { success = false, message = "您没有权限操作此资源" });
                        return;
                    }
                }
            }
        }

        await next();
    }
}
