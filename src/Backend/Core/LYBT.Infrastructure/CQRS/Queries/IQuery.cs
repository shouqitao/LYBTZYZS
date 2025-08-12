using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.CQRS.Queries
{
    /// <summary>
    /// 查询接口 - CQRS模式Query端
    /// UltraThink重构：实现读写分离，优化读操作性能
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    public interface IQuery<TResult> : IRequest<TResult>
    {
    }

    /// <summary>
    /// 查询处理器接口
    /// </summary>
    /// <typeparam name="TQuery">查询类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
    }

    /// <summary>
    /// 查询基类 - 提供通用属性
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    public abstract record QueryBase<TResult> : IQuery<TResult>
    {
        /// <summary>
        /// 查询ID - 用于追踪和缓存键生成
        /// </summary>
        public string QueryId { get; init; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// 执行用户ID
        /// </summary>
        public Guid? UserId { get; init; }

        /// <summary>
        /// 相关性ID - 用于分布式追踪
        /// </summary>
        public string CorrelationId { get; init; }

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; init; } = true;

        /// <summary>
        /// 缓存过期时间
        /// </summary>
        public TimeSpan? CacheExpiration { get; init; }

        /// <summary>
        /// 查询元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// 生成缓存键
        /// </summary>
        public virtual string GenerateCacheKey()
        {
            var type = GetType().Name;
            var hash = GetHashCode().ToString();
            return $"query:{type}:{hash}";
        }
    }

    /// <summary>
    /// 查询结果包装器
    /// </summary>
    /// <typeparam name="TData">数据类型</typeparam>
    public class QueryResult<TData>
    {
        public bool IsSuccess { get; init; }
        public TData Data { get; init; }
        public string ErrorMessage { get; init; }
        public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
        public string QueryId { get; init; }
        public bool FromCache { get; init; }
        public TimeSpan ExecutionTime { get; init; }

        /// <summary>
        /// 成功结果
        /// </summary>
        public static QueryResult<TData> Success(TData data, string queryId = null, bool fromCache = false, TimeSpan? executionTime = null)
        {
            return new QueryResult<TData>
            {
                IsSuccess = true,
                Data = data,
                QueryId = queryId,
                FromCache = fromCache,
                ExecutionTime = executionTime ?? TimeSpan.Zero
            };
        }

        /// <summary>
        /// 失败结果
        /// </summary>
        public static QueryResult<TData> Failure(string errorMessage, string queryId = null)
        {
            return new QueryResult<TData>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                QueryId = queryId
            };
        }
    }

    /// <summary>
    /// 分页查询基类
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    public abstract record PagedQueryBase<TResult> : QueryBase<PagedResult<TResult>>
    {
        /// <summary>
        /// 页码（从0开始）
        /// </summary>
        public int PageIndex { get; init; } = 0;

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchTerm { get; init; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string SortField { get; init; }

        /// <summary>
        /// 排序方向
        /// </summary>
        public string SortDirection { get; init; } = "desc";

        public override string GenerateCacheKey()
        {
            var baseKey = base.GenerateCacheKey();
            return $"{baseKey}:page:{PageIndex}:size:{PageSize}:search:{SearchTerm ?? "null"}:sort:{SortField ?? "default"}:{SortDirection}";
        }
    }
}