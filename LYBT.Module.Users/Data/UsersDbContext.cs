using System.Collections.Generic;
using System.Linq;
using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LYBT.Module.Users.Data {
    /// <summary>
    /// DbContext for Users module containing Users and AdminSecrets tables
    /// </summary>
    public class UsersDbContext : DbContext {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

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

            userEntity.Property(x => x.Roles)
                .HasConversion(
                    roles => string.Join(",", roles.Select(r => (int)r)),
                    value => ParseUserRoles(value))
                .Metadata.SetValueComparer(CreateListComparer<UserRole>());
        }

        private static List<UserRole> ParseUserRoles(string value) {
            if (string.IsNullOrEmpty(value))
                return new List<UserRole>();
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => int.TryParse(s, out _))
                .Select(s => (UserRole)int.Parse(s))
                .ToList();
        }

        private static ValueComparer<List<T>> CreateListComparer<T>() {
            return new ValueComparer<List<T>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c != null ? c.ToList() : new List<T>()
            );
        }
    }
}
