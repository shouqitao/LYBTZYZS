using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.ErrorHandling
{
    /// <summary>
    /// 标准异常处理器 - UltraThink架构
    /// </summary>
    public class StandardExceptionHandler
    {
        private readonly ILogger<StandardExceptionHandler> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        public StandardExceptionHandler(
            ILogger<StandardExceptionHandler> logger,
            IErrorHandlingService errorHandlingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        public async Task<bool> HandleAsync(Exception exception, string? context = null)
        {
            try
            {
                _logger.LogError(exception, "标准异常处理: {Context}", context ?? "未知上下文");

                await _errorHandlingService.HandleExceptionAsync(exception, null);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理异常时发生严重错误");
                return false;
            }
        }

        /// <summary>
        /// 同步处理异常
        /// </summary>
        public bool Handle(Exception exception, string? context = null)
        {
            try
            {
                _logger.LogError(exception, "标准异常处理: {Context}", context ?? "未知上下文");

                _errorHandlingService.HandleException(exception, null);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理异常时发生严重错误");
                return false;
            }
        }
    }
}
