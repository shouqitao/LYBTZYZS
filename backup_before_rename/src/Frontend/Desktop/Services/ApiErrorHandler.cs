using System;
using System.Threading.Tasks;
using Refit;
using LYBT.Desktop.Core.Models;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// API错误处理辅助类
    /// </summary>
    public static class ApiErrorHandler
    {
        /// <summary>
        /// 处理Refit API响应，转换为前端ServiceResult格式
        /// </summary>
        public static async Task<ServiceResult<T>> HandleApiResponseAsync<T>(Func<Task<Refit.ApiResponse<T>>> apiCall)
        {
            try
            {
                var response = await apiCall();

                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult<T>.Success(response.Content!);
                }
                else
                {
                    // 尝试从响应内容获取错误信息
                    var errorMessage = await ExtractErrorMessageAsync(response);
                    return ServiceResult<T>.Failure(errorMessage);
                }
            }
            catch (Refit.ApiException ex)
            {
                return await HandleApiExceptionAsync<T>(ex);
            }
            catch (Exception ex)
            {
                return ServiceResult<T>.Failure($"请求失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 处理Refit API异常
        /// </summary>
        private static async Task<ServiceResult<T>> HandleApiExceptionAsync<T>(Refit.ApiException ex)
        {
            var errorMessage = "请求失败";
            var statusCode = (int?)ex.StatusCode ?? 500;

            if (ex.HasContent)
            {
                try
                {
                    // 尝试解析为ProblemDetails
                    var problemDetails = await ex.GetContentAsAsync<LYBT.Desktop.Core.Models.ProblemDetails>();
                    if (problemDetails != null)
                    {
                        errorMessage = problemDetails.Detail ?? problemDetails.Title ?? ex.Message;
                    }
                }
                catch
                {
                    // 如果不是ProblemDetails格式，尝试获取原始内容
                    try
                    {
                        var content = await ex.GetContentAsAsync<string>();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            errorMessage = content;
                        }
                    }
                    catch
                    {
                        errorMessage = ex.Message;
                    }
                }
            }
            else
            {
                // 根据HTTP状态码提供更友好的错误消息
                errorMessage = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "未授权，请重新登录",
                    System.Net.HttpStatusCode.Forbidden => "无权限访问此资源",
                    System.Net.HttpStatusCode.NotFound => "请求的资源不存在",
                    System.Net.HttpStatusCode.BadRequest => "请求参数错误",
                    System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                    System.Net.HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                    System.Net.HttpStatusCode.GatewayTimeout => "请求超时",
                    _ => ex.Message
                };
            }

            return ServiceResult<T>.Failure(errorMessage, ex);
        }

        /// <summary>
        /// 从响应中提取错误消息
        /// </summary>
        private static Task<string> ExtractErrorMessageAsync<T>(Refit.ApiResponse<T> response)
        {
            if (response.Error != null)
            {
                try
                {
                    // 尝试解析错误内容为ProblemDetails
                    var content = response.Error.Content;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var problemDetails = System.Text.Json.JsonSerializer.Deserialize<LYBT.Desktop.Core.Models.ProblemDetails>(content, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (problemDetails != null)
                        {
                            return Task.FromResult(problemDetails.Detail ?? problemDetails.Title ?? "请求失败");
                        }
                    }
                }
                catch
                {
                    // 如果解析失败，返回原始错误消息
                }
            }

            // 根据状态码返回默认消息
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "未授权，请重新登录",
                System.Net.HttpStatusCode.Forbidden => "无权限访问此资源",
                System.Net.HttpStatusCode.NotFound => "请求的资源不存在",
                System.Net.HttpStatusCode.BadRequest => "请求参数错误",
                System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                System.Net.HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                System.Net.HttpStatusCode.GatewayTimeout => "请求超时",
                _ => "请求失败"
            };

            return Task.FromResult(errorMessage);
        }

        /// <summary>
        /// 简化的错误处理方法，用于不需要返回数据的操作
        /// </summary>
        public static async Task<ServiceResult> HandleApiCallAsync(Func<Task<Refit.ApiResponse<object>>> apiCall)
        {
            var result = await HandleApiResponseAsync(apiCall);
            if (result.IsSuccess)
            {
                return ServiceResult.Success();
            }
            else
            {
                return ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
            }
        }

        /// <summary>
        /// 简化的错误处理方法，用于不需要返回数据的操作（泛型版本）
        /// </summary>
        public static async Task<ServiceResult> HandleApiCallAsync<T>(Func<Task<Refit.ApiResponse<T>>> apiCall)
        {
            var result = await HandleApiResponseAsync(apiCall);
            if (result.IsSuccess)
            {
                return ServiceResult.Success();
            }
            else
            {
                return ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
            }
        }
    }
}