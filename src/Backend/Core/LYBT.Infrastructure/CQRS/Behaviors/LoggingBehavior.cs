using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Infrastructure.CQRS.Behaviors
{
    /// <summary>
    /// 日志记录行为管道 - UltraThink重构架构
    /// 自动记录所有CQRS操作的执行日志
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("开始处理 {RequestName}: {@Request}", requestName, SerializeRequest(request));

            TResponse response;
            try
            {
                response = await next();
                stopwatch.Stop();

                _logger.LogInformation("完成处理 {RequestName} - 耗时: {ElapsedMs}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "处理 {RequestName} 时发生错误 - 耗时: {ElapsedMs}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 安全序列化请求对象（避免敏感信息）
        /// </summary>
        private object SerializeRequest(TRequest request)
        {
            try
            {
                // 对于包含敏感信息的请求，进行安全处理
                var requestType = typeof(TRequest).Name;
                
                if (requestType.Contains("Password") || requestType.Contains("Login"))
                {
                    return new { RequestType = requestType, Message = "敏感信息已隐藏" };
                }

                // 普通请求可以完整序列化
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "序列化请求对象时发生错误: {RequestType}", typeof(TRequest).Name);
                return new { RequestType = typeof(TRequest).Name, Message = "序列化失败" };
            }
        }
    }
}