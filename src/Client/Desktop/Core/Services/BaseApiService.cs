using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Services.Exceptions;
using Polly;
using Polly.Timeout;
using Refit;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// API服务基类 - 提供统一的错误处理、重试和日志记录
    /// Desktop层精简架构的核心基类，替代复杂的三层结构
    /// </summary>
    /// <typeparam name="TApi">API接口类型</typeparam>
    public abstract class BaseApiService<TApi> where TApi : class
    {
        protected readonly TApi Api;
        protected readonly ILogger Logger;
        protected readonly IExceptionHandler ExceptionHandler;
        private readonly IAsyncPolicy<IApiResponse> _retryPolicy;

        protected BaseApiService(
            TApi api,
            ILogger logger,
            IExceptionHandler exceptionHandler)
        {
            Api = api ?? throw new ArgumentNullException(nameof(api));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            // 配置重试策略：3次重试，指数退避
            _retryPolicy = Policy
                .HandleResult<IApiResponse>(r => !r.IsSuccessStatusCode && IsRetriableError(r))
                .Or<TaskCanceledException>()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        Logger.LogWarning(
                            "API调用失败，正在重试 {RetryCount}/3，等待 {Timespan}秒",
                            retryCount, timespan.TotalSeconds);
                    });
        }

        /// <summary>
        /// 执行API调用（无返回值）
        /// </summary>
        protected async Task<ServiceResult> ExecuteApiCall(Func<Task<IApiResponse>> apiCall, string? operationName = null)
        {
            try
            {
                Logger.LogDebug("开始执行API调用: {Operation}", operationName ?? "Unknown");

                var response = await _retryPolicy.ExecuteAsync(async () => await apiCall());

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogDebug("API调用成功: {Operation}", operationName ?? "Unknown");
                    return ServiceResult.Success();
                }

                var error = await ExtractError(response);
                Logger.LogWarning("API调用失败: {Operation}, 错误: {Error}", operationName ?? "Unknown", error);
                return ServiceResult.Failure(error);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "API调用异常: {Operation}", operationName ?? "Unknown");
                return await HandleException(ex);
            }
        }

        /// <summary>
        /// 执行API调用（有返回值）
        /// </summary>
        protected async Task<ServiceResult<T>> ExecuteApiCall<T>(Func<Task<IApiResponse<T>>> apiCall, string? operationName = null)
        {
            try
            {
                Logger.LogDebug("开始执行API调用: {Operation}", operationName ?? "Unknown");

                // 由于Refit的IApiResponse<T>不直接继承IApiResponse，需要特殊处理
                var response = await apiCall();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Logger.LogDebug("API调用成功: {Operation}", operationName ?? "Unknown");
                    return ServiceResult<T>.Success(response.Content);
                }

                var error = await ExtractError(response);
                Logger.LogWarning("API调用失败: {Operation}, 错误: {Error}", operationName ?? "Unknown", error);
                return ServiceResult<T>.Failure(error);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "API调用异常: {Operation}", operationName ?? "Unknown");
                return await HandleException<T>(ex);
            }
        }

        /// <summary>
        /// 执行API调用（带缓存）
        /// </summary>
        protected async Task<ServiceResult<T>> ExecuteApiCallWithCache<T>(
            string cacheKey,
            Func<Task<IApiResponse<T>>> apiCall,
            TimeSpan? cacheDuration = null,
            string? operationName = null)
        {
            // 缓存功能可根据需要实现
            // 这里暂时直接调用API
            return await ExecuteApiCall(apiCall, operationName);
        }

        /// <summary>
        /// 判断是否为可重试的错误
        /// </summary>
        private bool IsRetriableError(IApiResponse response)
        {
            // 5xx 服务器错误和 429 Too Many Requests 可重试
            var statusCode = (int)response.StatusCode;
            return statusCode >= 500 || statusCode == 429;
        }

        /// <summary>
        /// 提取错误信息
        /// </summary>
        private async Task<string> ExtractError(IApiResponse response)
        {
            try
            {
                if (response.Error != null)
                {
                    var errorContent = await response.Error.GetContentAsAsync<ApiErrorResponse>();
                    return errorContent?.Message ?? response.ReasonPhrase ?? "未知错误";
                }
                return response.ReasonPhrase ?? $"HTTP {response.StatusCode}";
            }
            catch
            {
                return response.ReasonPhrase ?? $"HTTP {response.StatusCode}";
            }
        }

        /// <summary>
        /// 提取错误信息（泛型版本）
        /// </summary>
        private async Task<string> ExtractError<T>(IApiResponse<T> response)
        {
            try
            {
                if (response.Error != null)
                {
                    var errorContent = await response.Error.GetContentAsAsync<ApiErrorResponse>();
                    return errorContent?.Message ?? response.ReasonPhrase ?? "未知错误";
                }
                return response.ReasonPhrase ?? $"HTTP {response.StatusCode}";
            }
            catch
            {
                return response.ReasonPhrase ?? $"HTTP {response.StatusCode}";
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        private Task<ServiceResult> HandleException(Exception ex)
        {
            var result = ExceptionHandler.HandleException(ex, "ApiCall");
            return Task.FromResult(result);
        }

        /// <summary>
        /// 处理异常（泛型版本）
        /// </summary>
        private Task<ServiceResult<T>> HandleException<T>(Exception ex)
        {
            var result = ExceptionHandler.HandleException<T>(ex, "ApiCall");
            return Task.FromResult(result);
        }

        /// <summary>
        /// API错误响应模型
        /// </summary>
        private class ApiErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public object Details { get; set; }
        }
    }
}