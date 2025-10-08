using LYBT.Entities.MedicalCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// MedicalCase 实体 EF Core 配置
    /// </summary>
    public class MedicalCaseConfiguration : IEntityTypeConfiguration<MedicalCase>
    {
        public void Configure(EntityTypeBuilder<MedicalCase> entity)
        {
            entity.ToTable("MedicalCases");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasConversion<string>();
            entity.Property(m => m.Remark).HasMaxLength(500);
            entity.HasIndex(m => m.PatientId);
            entity.HasIndex(m => m.DoctorId);
            entity.HasIndex(m => m.Status);
            entity.HasIndex(m => m.CreatedAt);

            // 添加并发控制
            entity.Property(m => m.RowVersion).IsRowVersion().IsConcurrencyToken();

            // 添加审计字段
            entity.Property(m => m.CreatedBy).IsRequired();
            entity.Property(m => m.CreatedAt).IsRequired();

            // 根据文档要求：单患者仅一条未完成病案 - 过滤唯一索引
            // Status枚举值：Active=1, Completed=2, Cancelled=3
            entity.HasIndex(m => m.PatientId)
                  .HasDatabaseName("UX_MedicalCases_Patient_ActiveOnly")
                  .IsUnique()
                  .HasFilter("[Status] = 'Active'");

            // 删除PrescriptionId外键关系，改为通过Prescription.MedicalCaseId关联
            // 不再需要下面这行
            // entity.HasOne(m => m.Prescription).WithOne().HasForeignKey<MedicalCase>(m => m.PrescriptionId).IsRequired(false);
        }
    }
}
