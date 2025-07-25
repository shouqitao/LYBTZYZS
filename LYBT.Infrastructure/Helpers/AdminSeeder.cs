using LYBT.Common.Helpers;
using LYBT.Module.Users.Data;

namespace LYBT.Infrastructure.Helpers {

    /// <summary>
    /// Initializes default administrator account if not present.
    /// </summary>
    public static class AdminSeeder {

        /// <summary>
        /// 执行Seed操作。
        /// </summary>
        /// <param name="context">参数context</param>
        /// <param name="defaultPassword">参数defaultPassword</param>
        public static void Seed(UsersDbContext context, string defaultPassword) {
            // Default sysadmin credentials stored only in AdminSecrets table.
            // No corresponding record in Users table.
            if (!context.AdminSecrets.Any(s => s.UserName == "sysadmin")) {
                context.AdminSecrets.Add(new AdminSecretModel {
                    Id = Guid.NewGuid(),
                    UserName = "sysadmin",
                    PasswordHash = PasswordHelper.Hash(defaultPassword)
                });
            }

            if (context.ChangeTracker.HasChanges()) {
                context.SaveChanges();
            }
        }
    }
}