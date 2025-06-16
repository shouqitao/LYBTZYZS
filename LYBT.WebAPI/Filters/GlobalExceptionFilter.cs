using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.WebAPI.Filters {

    /// <summary>
    /// 全局异常过滤器
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter {

        public void OnException(ExceptionContext context) {
            context.Result = new JsonResult(new {
                code = 500,
                msg = "服务器发生异常，请联系管理员",
#if DEBUG
                detail = context.Exception.Message
#endif
            });
            context.ExceptionHandled = true;
        }
    }
}