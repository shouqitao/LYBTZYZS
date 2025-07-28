using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Data {
    /// <summary>
    /// DbContext for Users module containing Users and AdminSecrets tables
    /// </summary>
    public class UserDbContext : DbContext {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<AdminSecretModel> AdminSecrets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            ConfigureUserModule(modelBuilder);
        }

        private static void ConfigureUserModule(ModelBuilder modelBuilder) {
            var userEntity = modelBuilder.Entity<UserModel>();
            userEntity.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("IX_Users_UserName");
            userEntity.HasIndex(u => u.IsActive).HasDatabaseName("IX_Users_IsActive");
            userEntity.HasIndex(u => u.PinyinCode).HasDatabaseName("IX_Users_PinyinCode");
            userEntity.HasIndex(u => u.PhoneNumber).HasDatabaseName("IX_Users_PhoneNumber");

            // Configure Role property as enum
            userEntity.Property(x => x.Role)
                .HasConversion<int>()
                .HasDefaultValue(UserRole.Staff);
        }
    }
}
