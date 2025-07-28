using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Diagnostics.Data {

    /// <summary>
    /// 诊断治疗模块数据库上下文
    /// 此为模块化架构的基础 DbContext，实际的实体将在各自模块中定义
    /// </summary>
    public class DiagnosticDbContext : DbContext {

        public DiagnosticDbContext(DbContextOptions<DiagnosticDbContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // 具体的实体配置将在各自模块中通过 IEntityTypeConfiguration 实现
        }
    }
}