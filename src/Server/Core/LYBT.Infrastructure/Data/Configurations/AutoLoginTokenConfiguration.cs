using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// AutoLoginToken 实体 EF Core 配置
    /// CODE-18: 新建 Configuration 文件，补全 FK + 索引
    /// OpenSpec: refactor-login-authentication (CVT-001)
    /// </summary>
    public class AutoLoginTokenConfiguration : IEntityTypeConfiguration<AutoLoginToken>
    {
        public void Configure(EntityTypeBuilder<AutoLoginToken> entity)
        {
            entity.ToTable("AutoLoginTokens");
            entity.HasKey(e => e.Id);

            // 字符串长度由 Entity 的 [StringLength] 定义，遵循 DRY 原则
            entity.Property(e => e.Token).IsRequired();

            // Token 唯一索引（查询必需）
            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("IX_AutoLoginTokens_Token");

            // FamilyId 索引（重放攻击检测）
            entity.HasIndex(e => e.FamilyId)
                .HasDatabaseName("IX_AutoLoginTokens_FamilyId");

            // UserId + UserName 复合索引（登录查询）
            entity.HasIndex(e => new { e.UserId, e.UserName })
                .HasDatabaseName("IX_AutoLoginTokens_UserId_UserName");

            // AutoLoginToken -> User FK
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
