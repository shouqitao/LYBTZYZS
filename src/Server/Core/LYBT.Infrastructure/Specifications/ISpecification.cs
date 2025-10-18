using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Specifications
{
    /// <summary>
    /// Specification模式接口
    /// 用于封装复杂的查询逻辑，提供类型安全的查询组合
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface ISpecification<T> where T : class
    {
        /// <summary>
        /// 获取查询表达式
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// 获取Include表达式列表
        /// </summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// 获取字符串Include列表（向后兼容）
        /// </summary>
        List<string> IncludeStrings { get; }

        /// <summary>
        /// 获取排序表达式列表
        /// </summary>
        List<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderByClauses { get; }

        /// <summary>
        /// 获取分组表达式
        /// </summary>
        Expression<Func<T, object>>? GroupBy { get; }

        /// <summary>
        /// 获取分页参数
        /// </summary>
        (int Skip, int Take)? Pagination { get; }

        /// <summary>
        /// 是否启用AsNoTracking
        /// </summary>
        bool AsNoTracking { get; }

        /// <summary>
        /// 是否使用缓存
        /// </summary>
        bool UseCache { get; }

        /// <summary>
        /// 缓存过期时间（秒）
        /// </summary>
        int CacheExpirationSeconds { get; }
    }

    /// <summary>
    /// Specification扩展方法
    /// </summary>
    public static class SpecificationExtensions
    {
        /// <summary>
        /// 将Specification应用到IQueryable
        /// </summary>
        public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, ISpecification<T> specification) where T : class
        {
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // 应用Include表达式
            foreach (var include in specification.Includes)
            {
                query = query.Include(include);
            }

            // 应用字符串Include（向后兼容）
            foreach (var includeString in specification.IncludeStrings)
            {
                query = query.Include(includeString);
            }

            // 应用分组
            if (specification.GroupBy != null)
            {
                query = query.GroupBy(specification.GroupBy).SelectMany(g => g);
            }

            // 应用排序
            if (specification.OrderByClauses.Any())
            {
                IOrderedQueryable<T> orderedQuery = null;

                foreach (var (keySelector, ascending) in specification.OrderByClauses)
                {
                    if (orderedQuery == null)
                    {
                        orderedQuery = ascending
                            ? query.OrderBy(keySelector)
                            : query.OrderByDescending(keySelector);
                    }
                    else
                    {
                        orderedQuery = ascending
                            ? orderedQuery.ThenBy(keySelector)
                            : orderedQuery.ThenByDescending(keySelector);
                    }
                }

                query = orderedQuery ?? query;
            }

            // 应用分页
            if (specification.Pagination.HasValue)
            {
                query = query.Skip(specification.Pagination.Value.Skip)
                            .Take(specification.Pagination.Value.Take);
            }

            // 应用AsNoTracking
            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}