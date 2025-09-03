using Microsoft.AspNetCore.Identity;
using System;

// 生成正确的密码哈希
var hasher = new PasswordHasher<object>();
var password = "Admin@123456";
var hash = hasher.HashPassword(null!, password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");

// 验证测试
var verification = hasher.VerifyHashedPassword(null!, hash, password);
Console.WriteLine($"Verification: {verification}");