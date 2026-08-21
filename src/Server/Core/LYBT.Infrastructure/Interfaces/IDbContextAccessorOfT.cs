using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 泛型 DbContext 访问器（T3.1）
/// 避免直接注入具体 DbContext，模块通过此接口按需获取对应上下文
/// </summary>
/// <typeparam name="TContext">DbContext 类型</typeparam>
public interface IDbContextAccessor<TContext> where TContext : DbContext
{
    TContext Context { get; }
}
