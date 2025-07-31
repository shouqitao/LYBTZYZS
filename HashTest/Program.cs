using System;
using Microsoft.AspNetCore.Identity;

class Program
{
    static void Main()
    {
        var hasher = new PasswordHasher<object>();
        var password = "Admin@123456";
        var hash = hasher.HashPassword(null, password);
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        
        // 验证哈希
        var result = hasher.VerifyHashedPassword(null, hash, password);
        Console.WriteLine($"Verification: {result}");
    }
}
