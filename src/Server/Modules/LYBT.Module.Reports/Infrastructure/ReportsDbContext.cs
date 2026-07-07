using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 报表模块数据库上下文。只读模块，使用共享连接、独立Schema。
/// </summary>
public class ReportsDbContext : DbContext
{
    public ReportsDbContext(DbContextOptions<ReportsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Reports 模块使用 AppDbContext 的表，无需独立 Schema
    }
}


