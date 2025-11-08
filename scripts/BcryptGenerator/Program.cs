using BCrypt.Net;

Console.WriteLine("=== BCrypt密码哈希生成器与验证工具 ===");
Console.WriteLine();

// 明文密码
string adminPassword = "LybtAdmin2025@SecurePass!";

// 生成新哈希
string newHash = BCrypt.Net.BCrypt.HashPassword(adminPassword, 11);

// 配置文件中的旧哈希
string oldHash = "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C";

Console.WriteLine("=== 1. 验证配置文件中的旧哈希 ===");
Console.WriteLine($"明文密码: {adminPassword}");
Console.WriteLine($"旧哈希: {oldHash}");
bool oldValid = BCrypt.Net.BCrypt.Verify(adminPassword, oldHash);
Console.WriteLine($"验证结果: {(oldValid ? "✓ 成功 - 旧哈希可以验证明文密码" : "✗ 失败 - 旧哈希无法验证明文密码")}");
Console.WriteLine();

Console.WriteLine("=== 2. 生成新哈希并验证 ===");
Console.WriteLine($"明文密码: {adminPassword}");
Console.WriteLine($"新哈希: {newHash}");
bool newValid = BCrypt.Net.BCrypt.Verify(adminPassword, newHash);
Console.WriteLine($"验证结果: {(newValid ? "✓ 成功 - 新哈希可以验证明文密码" : "✗ 失败 - 新哈希无法验证明文密码")}");
Console.WriteLine();

Console.WriteLine("=== 3. 测试错误密码 ===");
string wrongPassword = "WrongPassword123!";
Console.WriteLine($"错误密码: {wrongPassword}");
Console.WriteLine($"旧哈希验证: {(BCrypt.Net.BCrypt.Verify(wrongPassword, oldHash) ? "✓ 成功" : "✗ 失败（预期）")}");
Console.WriteLine($"新哈希验证: {(BCrypt.Net.BCrypt.Verify(wrongPassword, newHash) ? "✓ 成功" : "✗ 失败（预期）")}");
Console.WriteLine();

Console.WriteLine("=== 4. Doctor1账号哈希 ===");
string doctorPassword = "Pass123!";
string doctorHash = BCrypt.Net.BCrypt.HashPassword(doctorPassword, 11);
Console.WriteLine($"用户名: doctor1");
Console.WriteLine($"密码: {doctorPassword}");
Console.WriteLine($"BCrypt哈希: {doctorHash}");
bool doctorVerified = BCrypt.Net.BCrypt.Verify(doctorPassword, doctorHash);
Console.WriteLine($"验证结果: {(doctorVerified ? "✓ 成功" : "✗ 失败")}");
Console.WriteLine();

Console.WriteLine("=== 总结 ===");
Console.WriteLine("如果旧哈希验证失败，说明配置文件中的哈希与明文密码不匹配。");
Console.WriteLine("请使用密码重置工具更新数据库中的密码哈希。");
