using LYBT.Common.Enums.Users;
using LYBT.Common.Helpers;
using LYBT.Module.Users.Models;

namespace LYBT.Infrastructure.Helpers {

    /// <summary>
    /// Initializes default administrator account if not present.
    /// </summary>
    public static class AdminSeeder {

        public static void Seed(AppDbContext context) {
            // Default sysadmin credentials stored only in AdminSecrets table.
            // No corresponding record in Users table.
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