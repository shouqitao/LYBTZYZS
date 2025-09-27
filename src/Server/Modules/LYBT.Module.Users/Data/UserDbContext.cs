using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Users.Data
{
    /// <summary>
    /// 用户模块数据库上下文
    /// 负责用户相关的数据访问，实现模块数据隔离
    /// </summary>
    public class UserDbContext : DbContext
    {
        private readonly ILogger<UserDbContext>? _logger;

        /// <summary>
        /// 用户实体集
        /// </summary>
        public DbSet<User> Users { get; set; } = default!;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 构造函数（带日志）
        /// </summary>
        /// <param name="options">数据库上下文选项</param>
        /// <param name="logger">日志记录器</param>
        public UserDbContext(DbContextOptions<UserDbContext> options, ILogger<UserDbContext> logger) 
            : base(options)
        {
            _logger = logger;
        }

        /// <summary>
        /// 配置模型
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置User实体
            ConfigureUserEntity(modelBuilder);

            // 应用模块特定的优化配置
            ApplyUserModuleOptimizations(modelBuilder);
        }

        /// <summary>
        /// 配置User实体
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            var userEntity = modelBuilder.Entity<User>();

            // 基础配置
            userEntity.ToTable("Users");
            userEntity.HasKey(u => u.Id);

            // 字段配置
            userEntity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            userEntity.Property(u => u.RealName)
                .HasMaxLength(100);

            userEntity.Property(u => u.PhoneNumber)
                .HasMaxLength(50);

            userEntity.Property(u => u.Role)
                .IsRequired()
                .HasConversion<int>(); // 枚举转换为整数

            // 索引配置
            userEntity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_User_Email");

            userEntity.HasIndex(u => u.PhoneNumber)
                .HasDatabaseName("IX_User_Phone");

            userEntity.HasIndex(u => u.Role)
                .HasDatabaseName("IX_User_Role");

            // 复合索引
            userEntity.HasIndex(u => new { u.IsDeleted, u.Role })
                .HasDatabaseName("IX_User_Deleted_Role");

            // 全局查询过滤器
            userEntity.HasQueryFilter(u => !u.IsDeleted);
        }

        /// <summary>
        /// 应用用户模块特定的优化配置
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        private void ApplyUserModuleOptimizations(ModelBuilder modelBuilder)
        {
            // 设置默认的字符串长度
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                    {
                        property.SetMaxLength(255);
                    }
                }
            }

            // 配置时间戳字段
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.Name == "CreatedAt" || p.Name == "UpdatedAt");

                foreach (var property in properties)
                {
                    property.SetColumnType("datetime2(3)"); // 毫秒精度
                }
            }
        }

        /// <summary>
        /// 保存更改时的附加逻辑
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>受影响的行数</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 在保存前更新时间戳
                UpdateTimestamps();

                var result = await base.SaveChangesAsync(cancellationToken);
                
                _logger?.LogDebug("用户模块数据库操作完成，受影响行数: {AffectedRows}", result);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "用户模块数据库保存操作失败");
                throw;
            }
        }

        /// <summary>
        /// 更新实体的时间戳
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is LYBT.Entities.Common.BaseEntity && 
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (LYBT.Entities.Common.BaseEntity)entry.Entity;
                
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}