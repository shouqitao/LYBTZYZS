using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// 模块 DbContext 共享扩展 — 软删除全局查询过滤器去重。
/// 各模块 DbContext 的 OnModelCreating 原先逐行写 <c>HasQueryFilter(e => !e.IsDeleted)</c>，
/// 与 AppDbContext.ApplyOptimizations 中的 ISoftDeletable 扫描逻辑重复；统一收敛到本扩展。
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// 为指定实体应用软删除全局查询过滤器（<c>IsDeleted == false</c>）。
    /// 链式调用以覆盖多个实体；非 <see cref="ISoftDeletable"/> 实体（如 FormulaHerbItem 基于父 Formula）
    /// 请在各模块 DbContext 中单独配置。
    /// </summary>
    public static ModelBuilder ApplySoftDeleteFilters<TEntity>(this ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        return modelBuilder;
    }
}
