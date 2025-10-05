using LYBT.Entities.Common;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Prescriptions;
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
        /// </summary>
        private static void OptimizePatientEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(entity =>
            {
                // 复合索引（常用查询条件组合）
                entity.HasIndex(p => new { p.Name, p.PhoneNumber })
                    .HasDatabaseName("IX_Patient_Name_Phone");

                entity.HasIndex(p => new { p.PinYinCode, p.IsDeleted })
                    .HasDatabaseName("IX_Patient_PinYin_Deleted");

                // 单列索引（频繁查询字段）
                entity.HasIndex(p => p.PhoneNumber)
                    .HasDatabaseName("IX_Patient_Phone");

                entity.HasIndex(p => p.IdNumber)
                    .HasDatabaseName("IX_Patient_IdNumber");

                entity.HasIndex(p => p.CreatedAt)
                    .HasDatabaseName("IX_Patient_CreatedAt");

                // 配置关系的延迟加载（避免N+1但允许按需加载）
                // 注意：当前实体未定义导航属性，暂时注释


                // entity.HasMany(p => p.Prescriptions)
                //     .WithOne(pr => pr.Patient)
                //     .HasForeignKey(pr => pr.PatientId)
                //     .OnDelete(DeleteBehavior.Restrict);
            });
        }

        /// <summary>
        /// 优化就诊记录实体配置
        /// </summary>
        private static void OptimizeMedicalCaseEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MedicalCase>(entity =>
            {
                // 复合索引（常用关联查询）
                entity.HasIndex(m => new { m.PatientId, m.CreatedAt })
                    .HasDatabaseName("IX_MedicalCase_Patient_Date");

                entity.HasIndex(m => new { m.DoctorId, m.Status })
                    .HasDatabaseName("IX_MedicalCase_Doctor_Status");

                entity.HasIndex(m => new { m.Status, m.CreatedAt })
                    .HasDatabaseName("IX_MedicalCase_Status_Date");

                // 配置关系

            });
        }

        /// <summary>
        /// 优化处方实体配置
        /// </summary>
        private static void OptimizePrescriptionEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Prescription>(entity =>
            {
                // 注释掉所有不存在的属性引用
                // 复合索引
                entity.HasIndex(p => new { p.PatientId, p.CreatedAt })
                    .HasDatabaseName("IX_Prescription_Patient_Date");

                entity.HasIndex(p => new { p.MedicalCaseId, p.Status })
                    .HasDatabaseName("IX_Prescription_MedicalCase_Status");

                // 注意：DoctorId属性不存在，注释掉
                // entity.HasIndex(p => new { p.DoctorId, p.CreatedAt })
                //     .HasDatabaseName("IX_Prescription_Doctor_Date");

                // 单列索引
                // 注意：PrescriptionNumber属性不存在，注释掉
                // entity.HasIndex(p => p.PrescriptionNumber)
                //     .IsUnique()
                //     .HasDatabaseName("IX_Prescription_Number");

                entity.HasIndex(p => p.Status)
                    .HasDatabaseName("IX_Prescription_Status");

            });
        }

        /// <summary>
        /// 优化用户实体配置
        /// </summary>
        private static void OptimizeUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                // 注释掉所有不存在的属性引用
                // 唯一索引
                // 注意：Username属性不存在，注释掉
                // entity.HasIndex(u => u.Username)
                //     .IsUnique()
                //     .HasDatabaseName("IX_User_Username");

                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_User_Email");

                entity.HasIndex(u => u.PhoneNumber)
                    .HasDatabaseName("IX_User_Phone");

                entity.HasIndex(u => u.Role)
                    .HasDatabaseName("IX_User_Role");


            });
        }

    }
}
