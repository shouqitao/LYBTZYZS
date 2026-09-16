using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// DbContext 审计字段自动化扩展（S-5 提取）。
/// 从 AppDbContext.SetAuditFields 提取为可复用静态扩展，
/// 供 AppDbContext 与各模块 DbContext（MedicalCaseDbContext / PatientsDbContext 等）共用。
/// </summary>
public static class DbContextAuditExtensions
{
    /// <summary>
    /// 为 ChangeTracker 中 Added / Modified 状态的 <see cref="IAuditableEntity"/> 实体自动填充审计字段。
    /// </summary>
    /// <param name="context">任意 DbContext</param>
    /// <param name="currentUserId">
    /// 当前操作用户 ID；null 时仅填充时间戳，不写 CreatedBy / UpdatedBy。
    /// AppDbContext 传入 HttpContext 解析结果（含 System 用户兜底）；模块 DbContext 可按需传入或传 null。
    /// </param>
    public static void SetAuditFields(this DbContext context, Guid? currentUserId)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        var timestamp = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var entity = (IAuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                // 强制设置 CreatedAt 和 UpdatedAt（统一由 DbContext 负责）
                entity.CreatedAt = timestamp;
                entity.UpdatedAt = timestamp;

                // 只在有用户上下文时设置 CreatedBy
                if (currentUserId.HasValue)
                {
                    entity.CreatedBy = currentUserId;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                // 强制设置 UpdatedAt
                entity.UpdatedAt = timestamp;

                // 只在有用户上下文时设置 UpdatedBy
                if (currentUserId.HasValue)
                {
                    entity.UpdatedBy = currentUserId;
                }
            }
        }
    }
}
