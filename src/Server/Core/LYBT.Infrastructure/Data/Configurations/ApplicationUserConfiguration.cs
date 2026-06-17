using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LYBT.Entities.Users;

namespace LYBT.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.RealName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastLoginAt).IsRequired(false);
    }
}
