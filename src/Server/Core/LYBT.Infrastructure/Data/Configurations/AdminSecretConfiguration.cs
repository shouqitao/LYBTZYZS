using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LYBT.Infrastructure.Data.Configurations
{
    /// <summary>
    /// AdminSecret 实体 EF Core 配置
    /// </summary>
    public class AdminSecretConfiguration : IEntityTypeConfiguration<AdminSecretModel>
    {
        public void Configure(EntityTypeBuilder<AdminSecretModel> entity)
        {
            entity.ToTable("AdminSecrets");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.PasswordHash).HasMaxLength(500).IsRequired();

            // Seed Data：添加默认的超级管理员种子数据
            // 使用固定ID，密码从配置文件指定的默认密码生成
            entity.HasData(new AdminSecretModel
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                PasswordHash = "AQAAAAIAAYagAAAAEBZtKH/jLrWSCIstrn4KyQtIopjqYQNrjJ8ZTIZxjKrpJ1l0obDU19hLQMSNwBjbeQ=="
            });
        }
    }
}
