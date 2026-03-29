using System;
using BCrypt.Net;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;

namespace PasswordHashGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string password = args.Length > 0 ? args[0] : "DevPass123";
            
            Console.WriteLine("=== LYBT Password Hash Generator ===");
            Console.WriteLine($"Input Password: {password}");
            Console.WriteLine();

            // 使用 PasswordHelper (SuperAdmin 角色)
            string hash = PasswordHelper.HashPassword(password, UserRole.SuperAdmin);
            
            Console.WriteLine($"Generated Hash: {hash}");
            Console.WriteLine($"Hash Length: {hash.Length}");
            Console.WriteLine();

            // 验证
            var result = PasswordHelper.VerifyPassword(password, hash, UserRole.SuperAdmin);
            Console.WriteLine($"Verification: {result.IsSuccess}");
            Console.WriteLine();

            // 输出 SQL
            Console.WriteLine("=== SQL for sysadmin ===");
            Console.WriteLine($"UPDATE Users SET PasswordHash = '{hash}', FailedLoginCount = 0, LockoutEnd = NULL, UpdatedAt = GETUTCDATE() WHERE UserName = 'sysadmin';");
        }
    }
}
