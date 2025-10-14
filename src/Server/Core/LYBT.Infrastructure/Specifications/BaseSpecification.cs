using System.Linq.Expressions;

namespace LYBT.Infrastructure.Specifications
{
    /// <summary>
    /// Specification基类实现
    /// 提供常用的Specification构建方法
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public abstract class BaseSpecification<T> : ISpecification<T> where T : class
    {
        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();
        public List<string> IncludeStrings { get; } = new List<string>();
        public List<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderByClauses { get; } = new List<(Expression<Func<T, object>>, bool)>();
        public Expression<Func<T, object>>? GroupBy { get; protected set; }
        public (int Skip, int Take)? Pagination { get; protected set; }
        public bool AsNoTracking { get; protected set; } = false;
        public bool UseCache { get; protected set; } = false;
        public int CacheExpirationSeconds { get; protected set; } = 300; // 默认5分钟缓存

        protected BaseSpecification()
        {
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        #region 构建器方法

        /// <summary>
        /// 添加Include表达式
        /// </summary>
        public BaseSpecification<T> WithInclude(Expression<Func<T, object>> include)
        {
            Includes.Add(include);
            return this;
        }

        /// <summary>
        /// 添加字符串Include
        /// </summary>
        public BaseSpecification<T> WithInclude(string include)
        {
            IncludeStrings.Add(include);
            return this;
        }

        /// <summary>
        /// 添加升序排序
        /// </summary>
        public BaseSpecification<T> OrderBy(Expression<Func<T, object>> keySelector)
        {
            OrderByClauses.Add((keySelector, true));
            return this;
        }

        /// <summary>
        /// 添加降序排序
        /// </summary>
        public BaseSpecification<T> OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            OrderByClauses.Add((keySelector, false));
            return this;
        }

        /// <summary>
        /// 添加ThenBy升序排序
        /// </summary>
        public BaseSpecification<T> ThenBy(Expression<Func<T, object>> keySelector)
        {
            OrderByClauses.Add((keySelector, true));
            return this;
        }

        /// <summary>
        /// 添加ThenBy降序排序
        /// </summary>
        public BaseSpecification<T> ThenByDescending(Expression<Func<T, object>> keySelector)
        {
            OrderByClauses.Add((keySelector, false));
            return this;
        }

        /// <summary>
        /// 设置分组
        /// </summary>
        public BaseSpecification<T> WithGroupBy(Expression<Func<T, object>> groupBy)
        {
            GroupBy = groupBy;
            return this;
        }

        /// <summary>
        /// 设置分页
        /// </summary>
        public BaseSpecification<T> WithPagination(int pageNumber, int pageSize)
        {
            Pagination = ((pageNumber - 1) * pageSize, pageSize);
            return this;
        }

        /// <summary>
        /// 设置Skip/Take分页
        /// </summary>
        public BaseSpecification<T> WithSkipTake(int skip, int take)
        {
            Pagination = (skip, take);
            return this;
        }

        /// <summary>
        /// 启用AsNoTracking
        /// </summary>
        public BaseSpecification<T> WithNoTracking()
        {
            AsNoTracking = true;
            return this;
        }

        /// <summary>
        /// 启用缓存
        /// </summary>
        public BaseSpecification<T> WithCache(int expirationSeconds = 300)
        {
            UseCache = true;
            CacheExpirationSeconds = expirationSeconds;
            return this;
        }

        #endregion
    }

    /// <summary>
    /// 直接Specification实现类
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class DirectSpecification<T> : BaseSpecification<T> where T : class
    {
        public DirectSpecification(Expression<Func<T, bool>> criteria) : base(criteria)
        {
        }
    }
}