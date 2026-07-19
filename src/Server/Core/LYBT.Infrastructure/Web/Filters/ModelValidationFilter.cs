using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 全局模型验证过滤器 — 自动返回 ApiResponse 格式的 400 错误
/// </summary>
public class ModelValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var controller = context.Controller as ControllerBase;
            if (controller != null)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var message = errors.Count > 0 ? $"参数验证失败: {string.Join("; ", errors)}" : "参数验证失败";
                context.Result = controller.ValidationFail(message);
                return;
            }
        }

        await next();
    }
}
