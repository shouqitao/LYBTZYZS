using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations.Base;

/// <summary>
/// Entity Configuration 基类
/// 统一配置 BaseEntity 字段：审计、并发控制、软删除
/// </summary>
/// <typeparam name="T">继承自 BaseEntity 的实体类型</typeparam>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // 主键配置
        builder.HasKey(e => e.Id);

        // 审计字段配置
        // 注意: GETUTCDATE() 是 SQL Server 特定语法
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.CreatedBy)
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .IsRequired(false);

        // 并发控制配置
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // 软删除配置
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // 软删除全局查询过滤器
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
