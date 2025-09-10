using Asp.Versioning;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Examples
{

    /// <summary>
    /// 多版本API控制器示例
    /// 展示如何在同一控制器中支持多个API版本
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0", Deprecated = true)] // v3.0已弃用
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class ExampleController : BaseSystemController
    {

        public ExampleController(ILogger<ExampleController> logger, IMemoryCache cache) : base(logger, cache)
        {
        }

        #region Version 1.0 APIs

        /// <summary>
        /// 获取数据 - v1.0
        /// </summary>
        /// <returns>返回v1.0格式的数据</returns>
        [HttpGet]
        [MapToApiVersion("1.0")]
        public IActionResult GetV1()
        {
            return Ok(new
            {
                version = "1.0",
                data = new
                {
                    id = 1,
                    name = "示例数据",
                    description = "这是API v1.0的响应格式"
                }
            });
        }

        /// <summary>
        /// 创建数据 - v1.0
        /// </summary>
        [HttpPost]
        [MapToApiVersion("1.0")]
        public IActionResult CreateV1([FromBody] CreateRequestV1 request)
        {
            // v1.0 创建逻辑
            return Ok(new
            {
                id = Guid.NewGuid(),
                name = request.Name,
                createdAt = DateTime.UtcNow
            });
        }

        #endregion Version 1.0 APIs

        #region Version 2.0 APIs

        /// <summary>
        /// 获取数据 - v2.0
        /// </summary>
        /// <returns>返回v2.0增强格式的数据</returns>
        [HttpGet]
        [MapToApiVersion("2.0")]
        public IActionResult GetV2()
        {
            return Ok(new
            {
                version = "2.0",
                data = new
                {
                    id = 1,
                    name = "示例数据",
                    description = "这是API v2.0的响应格式",
                    metadata = new
                    {
                        createdAt = DateTime.UtcNow,
                        updatedAt = DateTime.UtcNow,
                        tags = new[] { "sample", "v2" }
                    }
                },
                links = new
                {
                    self = "/api/v2/example/1",
                    related = "/api/v2/example/1/related"
                }
            });
        }

        /// <summary>
        /// 创建数据 - v2.0
        /// </summary>
        [HttpPost]
        [MapToApiVersion("2.0")]
        public IActionResult CreateV2([FromBody] CreateRequestV2 request)
        {
            // v2.0 创建逻辑，包含更多字段
            return Ok(new
            {
                id = Guid.NewGuid(),
                name = request.Name,
                description = request.Description,
                tags = request.Tags,
                metadata = new
                {
                    createdAt = DateTime.UtcNow,
                    createdBy = User.Identity?.Name
                }
            });
        }

        /// <summary>
        /// 批量操作 - v2.0新增功能
        /// </summary>
        [HttpPost("batch")]
        [MapToApiVersion("2.0")]
        public IActionResult BatchOperationV2([FromBody] BatchRequest request)
        {
            return Ok(new
            {
                processed = request.Items.Count,
                success = request.Items.Count,
                failed = 0,
                results = request.Items.Select(item => new
                {
                    id = Guid.NewGuid(),
                    status = "success"
                })
            });
        }

        #endregion Version 2.0 APIs

        #region Version-Neutral APIs

        /// <summary>
        /// 健康检查 - 所有版本通用
        /// </summary>
        [HttpGet("health")]
        [ApiVersionNeutral]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "neutral"
            });
        }

        #endregion Version-Neutral APIs
    }

    #region Request DTOs

    /// <summary>
    /// 创建请求 - v1.0
    /// </summary>
    public class CreateRequestV1
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 创建请求 - v2.0
    /// </summary>
    public class CreateRequestV2
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// 批量请求
    /// </summary>
    public class BatchRequest
    {
        public List<BatchItem> Items { get; set; } = new();
    }

    /// <summary>
    /// 批量项
    /// </summary>
    public class BatchItem
    {
        public string Operation { get; set; } = string.Empty;
        public object Data { get; set; } = new();
    }

    #endregion Request DTOs
}
