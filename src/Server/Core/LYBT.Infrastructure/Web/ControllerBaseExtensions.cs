using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Mvc;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// ControllerBase 扩展方法 — ApiResponse 包装和 Result 映射
/// </summary>
public static class ControllerBaseExtensions
{
    private static string GetRequestId(ControllerBase controller)
        => controller.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

    // ==================== 成功响应 ====================

    public static IActionResult Success(this ControllerBase controller, string message = "操作成功")
    {
        var response = ApiResponse.CreateSuccess(message: message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    public static IActionResult Success<T>(this ControllerBase controller, T data, string message = "操作成功")
    {
        var response = ApiResponse<T>.CreateSuccess(data, message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    public static IActionResult SuccessPaged<T>(this ControllerBase controller, PagedResult<T> pagedResult, string message = "查询成功")
    {
        var items = pagedResult.Items is List<T> list ? list : pagedResult.Items.ToList();
        var pageResult = new PagedResult<T>(items, pagedResult.TotalCount, pagedResult.CurrentPage, pagedResult.PageSize);
        var response = ApiResponse<PagedResult<T>>.CreateSuccess(pageResult, message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    // ==================== 错误响应 ====================

    public static IActionResult Error(this ControllerBase controller, string message)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.BadRequest(response);
    }

    public static IActionResult NotFoundResponse(this ControllerBase controller, string message = "资源未找到")
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.NotFound(response);
    }

    public static IActionResult BusinessFail(this ControllerBase controller, string message, string? errorCode = null)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        if (errorCode != null)
            response.Errors = new { code = errorCode };
        return controller.StatusCode(422, response);
    }

    public static IActionResult ValidationFail(this ControllerBase controller, string message = "参数验证失败")
    {
        var errors = controller.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        var response = ApiResponse.CreateFail(message, errors.Count > 0 ? errors : null);
        response.RequestId = GetRequestId(controller);
        return controller.BadRequest(response);
    }

    public static IActionResult ForbidResponse(this ControllerBase controller, string message)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.StatusCode(403, response);
    }

    // ==================== Result 映射 ====================

    public static IActionResult HandleResult<T>(this ControllerBase controller, Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
    {
        if (result.IsSuccess)
            return controller.Success(result.Data!, successMessage);

        var message = result.ErrorMessage ?? "操作失败";

        if (result.ModuleErrorCode.HasValue)
        {
            var moduleCode = result.ModuleErrorCode.Value;
            var httpStatus = moduleCode.ToHttpStatusCode();

            if (useAuthMapping)
            {
                var errorResponse = CreateModuleErrorResponse<T>(controller, message, moduleCode);
                return httpStatus switch
                {
                    401 => controller.Unauthorized(errorResponse),
                    403 => controller.StatusCode(403, errorResponse),
                    404 => controller.NotFound(errorResponse),
                    422 => controller.StatusCode(422, errorResponse),
                    503 => controller.StatusCode(503, errorResponse),
                    500 => controller.StatusCode(500, errorResponse),
                    _ => controller.StatusCode(httpStatus, errorResponse)
                };
            }

            var failResponse = ApiResponse.CreateFail(message);
            failResponse.RequestId = GetRequestId(controller);
            failResponse.Errors = new { code = moduleCode.ToFormattedString(), numericCode = (int)moduleCode };
            return controller.StatusCode(httpStatus, failResponse);
        }

        return controller.BusinessFail(message);
    }

    public static IActionResult HandleResult(this ControllerBase controller, Result result, string successMessage = "操作成功")
    {
        if (result.IsSuccess)
            return controller.Success(successMessage);

        var message = result.ErrorMessage ?? "操作失败";

        if (result.ModuleErrorCode.HasValue)
        {
            var moduleCode = result.ModuleErrorCode.Value;
            var httpStatus = moduleCode.ToHttpStatusCode();
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId(controller);
            response.Errors = new { code = moduleCode.ToFormattedString(), numericCode = (int)moduleCode };
            return controller.StatusCode(httpStatus, response);
        }

        return controller.BusinessFail(message);
    }

    private static ApiResponse<T> CreateModuleErrorResponse<T>(ControllerBase controller, string message, GenericErrorCode errorCode)
    {
        var response = ApiResponse<T>.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        response.Errors = new { code = errorCode.ToFormattedString(), numericCode = (int)errorCode };
        return response;
    }
}
