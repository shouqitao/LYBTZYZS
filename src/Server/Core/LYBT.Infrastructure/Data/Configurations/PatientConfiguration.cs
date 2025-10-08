using LYBT.Entities.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Patient 实体 EF Core 配置
    /// </summary>
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> entity)
        {
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100);

            entity.Property(p => p.PinYinCode).HasMaxLength(20);

            // CreateTime字段已删除（UltraThink v2.0简化）
            entity.Property(p => p.PhoneNumber).HasMaxLength(20);
            entity.Property(p => p.Address).HasMaxLength(256);
            entity.Property(p => p.IdType).HasMaxLength(20);
            entity.Property(p => p.IdNumber).HasMaxLength(50);

            // Occupation、MaritalStatus、Ethnicity、Education字段已删除
            entity.Property(p => p.AllergyHistory).HasMaxLength(500);

            // 配置Status枚举字段
            entity.Property(p => p.Status).HasConversion<int>();

            // 配置并发控制字段
            entity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();
        }
    }
}
