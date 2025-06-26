using LYBT.Common.Enums.Users;
using LYBT.Common.Helpers;
using LYBT.Module.Users.Models;

namespace LYBT.Infrastructure.Helpers {

    /// <summary>
    /// Initializes default administrator account if not present.
    /// </summary>
    public static class AdminSeeder {

        public static void Seed(AppDbContext context) {
            if (!context.Users.Any(u => u.UserName == "sysadmin")) {
                var admin = new UserModel {
                    Id = Guid.NewGuid(),
                    UserName = "sysadmin",
                    RealName = "系统管理员",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedTime = DateTime.Now,
                    PasswordHash = string.Empty
                };
                context.Users.Add(admin);
            }

            if (!context.AdminSecrets.Any(s => s.UserName == "sysadmin")) {
                context.AdminSecrets.Add(new AdminSecretModel {
                    Id = Guid.NewGuid(),
                    UserName = "sysadmin",
                    PasswordHash = PasswordHelper.Hash("SuperSecretKey12345")
                });
            }

            if (context.ChangeTracker.HasChanges()) {
                context.SaveChanges();
            }
        }
    }
}