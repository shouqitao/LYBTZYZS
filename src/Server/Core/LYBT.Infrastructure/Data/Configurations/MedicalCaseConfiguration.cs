using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations;

/// <summary>
/// MedicalCase 实体 EF Core 配置
/// 继承 BaseEntityConfiguration 统一审计字段和并发控制
/// </summary>
public class MedicalCaseConfiguration : BaseEntityConfiguration<MedicalCase>
{
    public override void Configure(EntityTypeBuilder<MedicalCase> builder)
    {
        base.Configure(builder);

        builder.ToTable("MedicalCases");

        // OpenSpec: fix-doctorid-to-userid - 统一使用UserId列名
        // 移除旧的DoctorId映射，数据库列名将与属性名一致
        // 需要运行迁移将DoctorId列重命名为UserId

        // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
        // Epic #2175 BF-002: NeedsPrescription字段配置（nullable支持三态）
        builder.Property(m => m.NeedsPrescription).IsRequired(false);

        // CreatedBy 必填（覆盖基类的可空配置）
        builder.Property(m => m.CreatedBy).IsRequired();

        // 根据文档要求：单患者仅一条未完成医案 - 过滤唯一索引
        builder.HasIndex(m => m.PatientId)
              .HasDatabaseName("UX_MedicalCases_Patient_ActiveOnly")
              .IsUnique()
              .HasFilter("[CaseStatus] = 1 AND [IsDeleted] = 0");

        // A2-03: 按医生查询医案的性能索引
        builder.HasIndex(m => m.UserId)
              .HasDatabaseName("IX_MedicalCases_UserId");

        // CODE-05/06: MedicalCase -> Patient FK (DDD 跨聚合 ID 引用，无导航属性)
        builder.HasOne<Patient>()
              .WithMany()
              .HasForeignKey(m => m.PatientId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Restrict);

        // CODE-05/06: MedicalCase -> User FK (DDD 跨聚合 ID 引用，无导航属性)
        builder.HasOne<User>()
              .WithMany()
              .HasForeignKey(m => m.UserId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Restrict);

        // ========== 打印管理字段配置 ==========
        builder.Property(m => m.PrintVersion).HasDefaultValue(1);
        builder.Property(m => m.PrintCount).HasDefaultValue(0);
        builder.Property(m => m.IsPrinted).HasDefaultValue(false);

        // PrintLogs 一对多关系 (Cascade 删除)
        builder.HasMany(m => m.PrintLogs)
              .WithOne(l => l.MedicalCase)
              .HasForeignKey(l => l.MedicalCaseId)
              .IsRequired()
              .OnDelete(DeleteBehavior.Cascade);
    }
}
