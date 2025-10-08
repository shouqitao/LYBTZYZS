#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var password = "Dev@Admin2025!";
var hash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine($"密码: {password}");
Console.WriteLine($"BCrypt 哈希: {hash}");
Console.WriteLine();
Console.WriteLine("SQL 更新语句:");
Console.WriteLine($"UPDATE AdminSecrets SET PasswordHash = '{hash}' WHERE Id = '00000000-0000-0000-0000-000000000001';");
