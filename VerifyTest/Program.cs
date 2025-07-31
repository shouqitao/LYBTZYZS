using System;
using Microsoft.AspNetCore.Identity;

class VerifyTest
{
    static void Main()
    {
        var hasher = new PasswordHasher<object>();
        var storedHash = "AQAAAAEAACcQAAAAECX50oayijdeIMB94pelx1dG9pic3YfcWAlHiyZR8ITgQJBYLRiwAOJ7s+uJkxoCOA==";
        var password = "Admin@123456";
        
        var result = hasher.VerifyHashedPassword(null, storedHash, password);
        Console.WriteLine($"Verification result: {result}");
        
        // Also test the PasswordHelper methods
        Console.WriteLine("Testing manually stored hash against password...");
        var isValid = (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded);
        Console.WriteLine($"Password valid: {isValid}");
    }
}