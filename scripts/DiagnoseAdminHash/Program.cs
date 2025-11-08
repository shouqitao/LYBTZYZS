using Microsoft.Data.SqlClient;
using BCrypt.Net;

/// <summary>
/// SysAdmin密码哈希诊断工具
/// Issue #1908: 诊断sysadmin无法登录的问题
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===== SysAdmin密码哈希诊断工具 =====");
        Console.WriteLine();

        string connectionString = args.Length > 0
            ? args[0]
            : "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true";

        string expectedPassword = "LybtAdmin2025@SecurePass!";
        string expectedHash = "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C";

        try
        {
            // 连接数据库
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("✓ 数据库连接成功");
            Console.WriteLine();

            // 查询AdminSecrets表
            var query = @"
SELECT Id, PasswordHash
FROM AdminSecrets
WHERE Id = '00000000-0000-0000-0000-000000000001'";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var id = reader["Id"].ToString();
                var actualHash = reader["PasswordHash"].ToString() ?? string.Empty;

                Console.WriteLine("=== 数据库中的AdminSecrets ===");
                Console.WriteLine($"ID: {id}");
                Console.WriteLine($"实际哈希: {actualHash}");
                Console.WriteLine($"哈希长度: {actualHash.Length}");
                Console.WriteLine();

                Console.WriteLine("=== 期望值（配置文件） ===");
                Console.WriteLine($"期望哈希: {expectedHash}");
                Console.WriteLine($"期望密码: {expectedPassword}");
                Console.WriteLine();

                Console.WriteLine("=== 哈希对比 ===");
                bool hashMatch = actualHash == expectedHash;
                Console.WriteLine($"哈希是否匹配: {(hashMatch ? "✓ 一致" : "✗ 不一致")}");
                Console.WriteLine();

                Console.WriteLine("=== BCrypt验证测试 ===");

                // 测试实际哈希能否验证期望密码
                if (!string.IsNullOrEmpty(actualHash))
                {
                    try
                    {
                        bool actualValid = BCrypt.Net.BCrypt.Verify(expectedPassword, actualHash);
                        Console.WriteLine($"实际哈希验证期望密码: {(actualValid ? "✓ 成功" : "✗ 失败")}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"实际哈希验证失败: {ex.Message}");
                    }
                }

                // 测试期望哈希能否验证期望密码
                bool expectedValid = BCrypt.Net.BCrypt.Verify(expectedPassword, expectedHash);
                Console.WriteLine($"期望哈希验证期望密码: {(expectedValid ? "✓ 成功" : "✗ 失败")}");
                Console.WriteLine();

                // 诊断结果
                Console.WriteLine("=== 诊断结果 ===");
                if (hashMatch)
                {
                    if (expectedValid)
                    {
                        Console.WriteLine("✓ 数据库哈希正确，可以验证密码 'LybtAdmin2025@SecurePass!'");
                        Console.WriteLine();
                        Console.WriteLine("建议：");
                        Console.WriteLine("  1. 确认登录时输入的密码完全正确（区分大小写）");
                        Console.WriteLine("  2. 检查Server端日志，查看认证失败的详细原因");
                        Console.WriteLine("  3. 确认AuthService.VerifySysAdminCredentialsAsync是否被正确调用");
                    }
                    else
                    {
                        Console.WriteLine("✗ 期望哈希无法验证期望密码（配置错误）");
                        Console.WriteLine();
                        Console.WriteLine("解决方案：");
                        Console.WriteLine("  使用密码重置工具更新数据库密码：");
                        Console.WriteLine("  dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t sysadmin -p \"LybtAdmin2025@SecurePass!\"");
                    }
                }
                else
                {
                    Console.WriteLine("✗ 数据库哈希与配置文件不一致");
                    Console.WriteLine();
                    Console.WriteLine("可能原因：");
                    Console.WriteLine("  1. 数据库未使用最新的种子数据（Migration未执行）");
                    Console.WriteLine("  2. 密码已被手动修改");
                    Console.WriteLine();
                    Console.WriteLine("解决方案：");
                    Console.WriteLine("  使用密码重置工具更新数据库密码：");
                    Console.WriteLine("  dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t sysadmin -p \"LybtAdmin2025@SecurePass!\"");
                }
            }
            else
            {
                Console.WriteLine("✗ AdminSecrets表中未找到sysadmin记录");
                Console.WriteLine();
                Console.WriteLine("解决方案：");
                Console.WriteLine("  1. 检查数据库Migration是否已执行");
                Console.WriteLine("  2. 执行 dotnet ef database update");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 错误: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine($"详细信息: {ex}");
        }

        Console.WriteLine();
        Console.WriteLine("===== 完成 =====");
    }
}
