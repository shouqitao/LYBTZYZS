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
            // Issue #1074: 使用BCrypt格式哈希，与AuthService验证逻辑一致
            // 密码: LybtAdmin2025@SecurePass!
            // BCrypt哈希使用: BCrypt.Net.BCrypt.HashPassword("LybtAdmin2025@SecurePass!", 11)
            entity.HasData(new AdminSecretModel
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                PasswordHash = "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C"
            });
        }
    }
}
