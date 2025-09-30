using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 标准错误处理器接口
    /// </summary>
    public interface IStandardErrorHandler
    {
        void HandleError(Exception exception, string context);
        Task HandleErrorAsync(Exception exception, string context);
        string GetErrorMessage(Exception exception);
    }

    /// <summary>
    /// 标准错误处理器实现 - UltraThink架构
    /// </summary>
    public class StandardErrorHandler : IStandardErrorHandler
    {
        private readonly ILogger<StandardErrorHandler> _logger;

        public StandardErrorHandler(ILogger<StandardErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void HandleError(Exception exception, string context)
        {
            _logger.LogError(exception, "错误发生在: {Context}", context);
        }

        public async Task HandleErrorAsync(Exception exception, string context)
        {
            await Task.Run(() => HandleError(exception, context));
        }

        public string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException => "参数错误",
                InvalidOperationException => "操作无效",
                UnauthorizedAccessException => "访问被拒绝",
                _ => "发生未知错误"
            };
        }
    }
}