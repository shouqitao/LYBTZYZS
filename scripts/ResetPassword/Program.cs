using Microsoft.Data.SqlClient;
using BCrypt.Net;

Console.WriteLine("===== 重置用户密码工具 =====");
Console.WriteLine();

// 默认参数
var username = args.Length > 0 ? args[0] : "shouqitao";
var newPassword = args.Length > 1 ? args[1] : "Lybt2025@TempPass!";
var connectionString = args.Length > 2 ? args[2] : "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true";

Console.WriteLine($"目标用户: {username}");
Console.WriteLine();

try
{
    // 生成密码哈希
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    Console.WriteLine("✓ 密码哈希已生成");

    // 连接数据库
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✓ 数据库连接成功");
    Console.WriteLine();

    // 查询用户信息
    var queryUser = @"
SELECT Id, Username, Email, RealName, Status
FROM Users
WHERE Username = @Username";

    using var cmdQuery = new SqlCommand(queryUser, connection);
    cmdQuery.Parameters.AddWithValue("@Username", username);

    using var reader = await cmdQuery.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        var userId = reader["Id"];
        var email = reader["Email"];
        var realName = reader["RealName"];
        var status = reader["Status"];

        Console.WriteLine("找到用户信息:");
        Console.WriteLine($"  ID: {userId}");
        Console.WriteLine($"  用户名: {username}");
        Console.WriteLine($"  邮箱: {email}");
        Console.WriteLine($"  真实姓名: {realName}");
        Console.WriteLine($"  状态: {status}");
        Console.WriteLine();

        await reader.CloseAsync();

        // 更新密码
        var updatePassword = @"
UPDATE Users
SET PasswordHash = @PasswordHash
WHERE Id = @UserId";

        using var cmdUpdate = new SqlCommand(updatePassword, connection);
        cmdUpdate.Parameters.AddWithValue("@PasswordHash", passwordHash);
        cmdUpdate.Parameters.AddWithValue("@UserId", userId);

        var rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            Console.WriteLine("✓ 密码重置成功!");
            Console.WriteLine();
            Console.WriteLine("登录信息:");
            Console.WriteLine($"  用户名: {username}");
            Console.WriteLine($"  新密码: {newPassword}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("✗ 密码重置失败");
        }
    }
    else
    {
        Console.WriteLine($"✗ 未找到用户: {username}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 错误: {ex.Message}");
    Console.WriteLine($"详细信息: {ex}");
}

Console.WriteLine("===== 完成 =====");
