using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure
{

    /// <summary>
    /// 规约模式基类 - UltraThink查询优化
    ///
    /// 实现复杂查询的组合和复用
    /// </summary>
    public abstract class Specification<T>
    {

        public abstract Expression<Func<T, bool>> ToExpression();

        public bool IsSatisfiedBy(T entity)
        {
            var predicate = ToExpression().Compile();
            return predicate(entity);
        }

        public Specification<T> And(Specification<T> specification)
        {
            return new AndSpecification<T>(this, specification);
        }

        public Specification<T> Or(Specification<T> specification)
        {
            return new OrSpecification<T>(this, specification);
        }

        public Specification<T> Not()
        {
            return new NotSpecification<T>(this);
        }

        public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
        {
            return specification.ToExpression();
        }
    }

    /// <summary>
    /// AND组合规约
    /// </summary>
    public class AndSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;

        public AndSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpression = _left.ToExpression();
            var rightExpression = _right.ToExpression();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(leftExpression, parameter),
                Expression.Invoke(rightExpression, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    /// <summary>
    /// OR组合规约
    /// </summary>
    public class OrSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;

        public OrSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left;
            _right = right;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpression = _left.ToExpression();
            var rightExpression = _right.ToExpression();

            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.OrElse(
                Expression.Invoke(leftExpression, parameter),
                Expression.Invoke(rightExpression, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    /// <summary>
    /// NOT规约
    /// </summary>
    public class NotSpecification<T> : Specification<T>
    {
        private readonly Specification<T> _specification;

        public NotSpecification(Specification<T> specification)
        {
            _specification = specification;
        }

        public override Expression<Func<T, bool>> ToExpression()
        {
            var expression = _specification.ToExpression();
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.Not(Expression.Invoke(expression, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    /// <summary>
    /// 查询规约 - 包含完整的查询选项
    /// </summary>
    public class QuerySpecification<T> : Specification<T>
    {
        private readonly List<Expression<Func<T, object>>> _includes = new();
        private readonly List<string> _includeStrings = new();
        private Expression<Func<T, bool>>? _criteria;
        private Expression<Func<T, object>>? _orderBy;
        private Expression<Func<T, object>>? _orderByDescending;
        private Expression<Func<T, object>>? _groupBy;

        public QuerySpecification()
        {
        }

        public QuerySpecification(Expression<Func<T, bool>> criteria)
        {
            _criteria = criteria;
        }

        public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
        public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();
        public Expression<Func<T, object>>? OrderBy => _orderBy;
        public Expression<Func<T, object>>? OrderByDescending => _orderByDescending;
        public Expression<Func<T, object>>? GroupBy => _groupBy;
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IsDistinct { get; private set; }

        public override Expression<Func<T, bool>> ToExpression()
        {
            return _criteria ?? (x => true);
        }

        public QuerySpecification<T> Where(Expression<Func<T, bool>> criteria)
        {
            _criteria = _criteria == null
                ? criteria
                : Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(_criteria.Body, criteria.Body),
                    _criteria.Parameters);
            return this;
        }

        public QuerySpecification<T> Include(Expression<Func<T, object>> includeExpression)
        {
            _includes.Add(includeExpression);
            return this;
        }

        public QuerySpecification<T> Include(string includeString)
        {
            _includeStrings.Add(includeString);
            return this;
        }

        public QuerySpecification<T> OrderByAscending(Expression<Func<T, object>> orderByExpression)
        {
            _orderBy = orderByExpression;
            _orderByDescending = null;
            return this;
        }

        public QuerySpecification<T> ApplyOrderByDescending(Expression<Func<T, object>> orderByExpression)
        {
            _orderByDescending = orderByExpression;
            _orderBy = null;
            return this;
        }

        public QuerySpecification<T> GroupByExpression(Expression<Func<T, object>> groupByExpression)
        {
            _groupBy = groupByExpression;
            return this;
        }

        public QuerySpecification<T> ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
            return this;
        }

        public QuerySpecification<T> ApplyDistinct()
        {
            IsDistinct = true;
            return this;
        }
    }

    /// <summary>
    /// 规约评估器 - 将规约应用到查询
    /// </summary>
    public static class SpecificationEvaluator<T> where T : class
    {

        public static IQueryable<T> GetQuery(
            IQueryable<T> inputQuery,
            QuerySpecification<T> specification)
        {
            var query = inputQuery;

            // 应用过滤条件
            if (specification.ToExpression() != null)
            {
                query = query.Where(specification.ToExpression());
            }

            // 应用Include
            query = specification.Includes.Aggregate(
                query,
                (current, include) => current.Include(include));

            // 应用字符串Include
            query = specification.IncludeStrings.Aggregate(
                query,
                (current, include) => current.Include(include));

            // 应用排序
            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            // 应用分组
            if (specification.GroupBy != null)
            {
                query = query.GroupBy(specification.GroupBy).SelectMany(g => g);
            }

            // 应用分页
            if (specification.IsPagingEnabled)
            {
                if (specification.Skip.HasValue)
                {
                    query = query.Skip(specification.Skip.Value);
                }

                if (specification.Take.HasValue)
                {
                    query = query.Take(specification.Take.Value);
                }
            }

            // 应用Distinct
            if (specification.IsDistinct)
            {
                query = query.Distinct();
            }

            return query;
        }
    }

    /// <summary>
    /// 通用规约实现示例
    /// </summary>
    public static class CommonSpecifications
    {

        /// <summary>
        /// 日期范围规约
        /// </summary>
        public class DateRangeSpecification<T> : Specification<T>
        {
            private readonly Expression<Func<T, DateTime>> _dateSelector;
            private readonly DateTime _startDate;
            private readonly DateTime _endDate;

            public DateRangeSpecification(
                Expression<Func<T, DateTime>> dateSelector,
                DateTime startDate,
                DateTime endDate)
            {
                _dateSelector = dateSelector;
                _startDate = startDate;
                _endDate = endDate;
            }

            public override Expression<Func<T, bool>> ToExpression()
            {
                var parameter = Expression.Parameter(typeof(T));
                var dateProperty = Expression.Invoke(_dateSelector, parameter);

                var startComparison = Expression.GreaterThanOrEqual(
                    dateProperty,
                    Expression.Constant(_startDate));

                var endComparison = Expression.LessThanOrEqual(
                    dateProperty,
                    Expression.Constant(_endDate));

                var body = Expression.AndAlso(startComparison, endComparison);

                return Expression.Lambda<Func<T, bool>>(body, parameter);
            }
        }

        /// <summary>
        /// 分页规约
        /// </summary>
        public class PaginationSpecification<T> : QuerySpecification<T>
        {

            public PaginationSpecification(int pageNumber, int pageSize)
            {
                ApplyPaging((pageNumber - 1) * pageSize, pageSize);
            }
        }

        /// <summary>
        /// 包含删除标记的规约
        /// </summary>
        public class NotDeletedSpecification<T> : Specification<T>
        {

            public override Expression<Func<T, bool>> ToExpression()
            {
                return entity => !EF.Property<bool>(entity!, "IsDeleted");
            }
        }

        /// <summary>
        /// 活跃记录规约
        /// </summary>
        public class ActiveSpecification<T> : Specification<T>
        {

            public override Expression<Func<T, bool>> ToExpression()
            {
                return entity => EF.Property<bool>(entity!, "IsActive");
            }
        }
    }
}
