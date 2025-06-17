using System;
using System.Linq;
using LYBT.Module.Users.Models;
using LYBT.Common.Enums.Users;

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

                context.SaveChanges();
            }
        }

        private static string HashPassword(string password) {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }
    }
}
