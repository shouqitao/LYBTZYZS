using BCrypt.Net;

// 生成sysadmin密码的BCrypt哈希
string adminPassword = "LybtAdmin2025@SecurePass!";
string adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, 11);

Console.WriteLine("=== BCrypt密码哈希生成器 ===");
Console.WriteLine();
Console.WriteLine("SysAdmin账号:");
Console.WriteLine($"  用户名: sysadmin");
Console.WriteLine($"  密码: {adminPassword}");
Console.WriteLine($"  BCrypt哈希: {adminHash}");

// 验证哈希
bool adminVerified = BCrypt.Net.BCrypt.Verify(adminPassword, adminHash);
Console.WriteLine($"  验证结果: {(adminVerified ? "✓ 成功" : "✗ 失败")}");

Console.WriteLine();

// 生成doctor1密码的哈希
string doctorPassword = "Pass123!";
string doctorHash = BCrypt.Net.BCrypt.HashPassword(doctorPassword, 11);

Console.WriteLine("Doctor1账号:");
Console.WriteLine($"  用户名: doctor1");
Console.WriteLine($"  密码: {doctorPassword}");
Console.WriteLine($"  BCrypt哈希: {doctorHash}");

bool doctorVerified = BCrypt.Net.BCrypt.Verify(doctorPassword, doctorHash);
Console.WriteLine($"  验证结果: {(doctorVerified ? "✓ 成功" : "✗ 失败")}");

Console.WriteLine();
Console.WriteLine("请将上述哈希值更新到UserConfiguration.cs的种子数据中。");
