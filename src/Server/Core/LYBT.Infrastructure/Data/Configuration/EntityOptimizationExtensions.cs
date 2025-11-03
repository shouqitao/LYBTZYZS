using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data.Configuration
{
    /// <summary>
    /// 实体映射优化扩展
    /// 提供查询优化相关的配置，包括索引、全局查询过滤器和关系加载策略
    /// </summary>
    public static class EntityOptimizationExtensions
    {
        /// <summary>
        /// 应用所有优化配置
        /// </summary>
        public static void ApplyOptimizations(this ModelBuilder modelBuilder)
        {
            // 应用全局查询过滤器
            ApplyGlobalQueryFilters(modelBuilder);

            // 优化患者实体配置
            OptimizePatientEntity(modelBuilder);

            // 优化就诊记录实体配置
            OptimizeMedicalCaseEntity(modelBuilder);

            // 优化处方实体配置
            OptimizePrescriptionEntity(modelBuilder);

            // 优化用户实体配置
            OptimizeUserEntity(modelBuilder);
        }

        /// <summary>
        /// 应用全局查询过滤器（自动过滤软删除的数据）
        /// </summary>
        private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // 为所有实现BaseEntity的类型添加全局过滤器
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(EntityOptimizationExtensions)
                        .GetMethod(nameof(ConfigureGlobalQueryFilter),
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(null, new object[] { modelBuilder });
                }
            }
        }

        /// <summary>
        /// 配置全局查询过滤器泛型方法
        /// </summary>
        private static void ConfigureGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : BaseEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        /// <summary>
        /// 优化患者实体配置
        /// Issue #1763: 简化索引策略，仅保留MVP阶段必需的索引
        /// </summary>
        private static void OptimizePatientEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                // MVP阶段仅保留手机号查询索引（业务必需）
                entity.HasIndex(p => p.PhoneNumber)
                    .HasDatabaseName("IX_Patient_Phone");
            });
        }

        /// <summary>
        /// 优化就诊记录实体配置
        /// Issue #1763: MVP阶段(<10K记录)无需额外索引，主键和外键索引已足够
        /// </summary>
        private static void OptimizeMedicalCaseEntity(ModelBuilder modelBuilder)
        {
            // MVP阶段不创建额外索引
            // 数据规模达到10万+时再考虑添加复合索引
        }

        /// <summary>
        /// 优化处方实体配置
        /// Issue #1763: MVP阶段(<10K记录)无需额外索引，主键和外键索引已足够
        /// </summary>
        private static void OptimizePrescriptionEntity(ModelBuilder modelBuilder)
        {
            // MVP阶段不创建额外索引
            // 数据规模达到10万+时再考虑添加复合索引
        }

        /// <summary>
        /// 优化用户实体配置
        /// Issue #1763: 仅保留登录和查询必需的索引
        /// </summary>
        private static void OptimizeUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                // 邮箱唯一索引（登录必需）
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_User_Email");

                // 手机号索引（可能用于查询和登录）
                entity.HasIndex(u => u.PhoneNumber)
                    .HasDatabaseName("IX_User_Phone");
            });
        }

    }
}
