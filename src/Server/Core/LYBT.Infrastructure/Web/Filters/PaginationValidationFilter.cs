using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 分页参数验证过滤器 — 从 ActionArguments 自动验证 page/pageSize
/// 使用方式: [ServiceFilter(typeof(PaginationValidationFilter))]
/// </summary>
public class PaginationValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.TryGetValue("page", out var pageObj) &&
            context.ActionArguments.TryGetValue("pageSize", out var pageSizeObj) &&
            pageObj is int page && pageSizeObj is int pageSize)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                var controller = context.Controller as ControllerBase;
                if (controller != null)
                {
                    context.Result = controller.ValidationFail("分页参数无效：page 和 pageSize 必须大于 0，pageSize 不能超过 100");
                    return;
                }
            }
        }

        await next();
    }
}
