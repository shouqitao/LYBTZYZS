using System;
using Microsoft.AspNetCore.Identity;

class Program
{
    static void Main()
    {
        var hasher = new PasswordHasher<object>();
        var hash = hasher.HashPassword(null, "Admin@123456");
        Console.WriteLine(hash);
    }
}