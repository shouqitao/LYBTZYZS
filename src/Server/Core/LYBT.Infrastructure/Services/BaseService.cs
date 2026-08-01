using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// Service 基类 — 提供统一的 ILogger 注入
    /// 权限验证已统一到 BaseApiController.ValidateOwnership，此处不再重复
    /// </summary>
    public abstract class BaseService
    {
        protected readonly ILogger _logger;

        protected BaseService(ILogger logger)
        {
            _logger = logger;
        }
    }

    /// <summary>
    /// 泛型 Service 基类 — 提供类型安全的 Logger
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseService<T> : BaseService where T : class
    {
        protected BaseService(ILogger logger) : base(logger)
        {
        }
    }
}
