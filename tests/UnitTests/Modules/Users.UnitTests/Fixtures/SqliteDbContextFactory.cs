using LYBT.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LYBT.Module.Users.Tests.Fixtures
{
    /// <summary>
    /// SQLite DbContext 工厂，用于创建兼容的测试数据库上下文
    /// </summary>
    public static class SqliteDbContextFactory
    {
        /// <summary>
        /// 创建 SQLite 兼容的 AppDbContext
        /// </summary>
        public static AppDbContext CreateContext(SqliteConnection connection)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(connection);
            optionsBuilder.EnableSensitiveDataLogging();

            // 创建一个自定义的 AppDbContext，重写 SaveChangesAsync 来处理 RowVersion
            return new SqliteAppDbContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// SQLite 专用的 AppDbContext
    /// </summary>
    internal class SqliteAppDbContext : AppDbContext
    {
        public SqliteAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 修改所有实体的 RowVersion 配置
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var rowVersionProperty = entityType.FindProperty("RowVersion");
                if (rowVersionProperty != null)
                {
                    // SQLite 不支持自动生成 rowversion
                    rowVersionProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                }
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 处理 RowVersion 初始化
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    var rowVersionProperty = entry.Properties
                        .FirstOrDefault(p => p.Metadata.Name == "RowVersion");

                    if (rowVersionProperty != null && rowVersionProperty.CurrentValue == null)
                    {
                        rowVersionProperty.CurrentValue = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    var rowVersionProperty = entry.Properties
                        .FirstOrDefault(p => p.Metadata.Name == "RowVersion");

                    if (rowVersionProperty != null && rowVersionProperty.CurrentValue is byte[] currentValue)
                    {
                        // 模拟 RowVersion 递增
                        var newValue = new byte[currentValue.Length];
                        currentValue.CopyTo(newValue, 0);
                        if (newValue.Length > 0)
                        {
                            newValue[newValue.Length - 1]++;
                        }
                        rowVersionProperty.CurrentValue = newValue;
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}