using Microsoft.Data.SqlClient;
using BCrypt.Net;

/// <summary>
/// 密码重置工具 - 支持sysadmin和普通用户
/// Issue #1908: 增强密码重置工具,支持AdminSecrets表的sysadmin账户
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("===== LYBTZYZS 密码重置工具 =====");
        Console.WriteLine("Issue #1908: 支持sysadmin和普通用户密码重置");
        Console.WriteLine();

        try
        {
            // 解析命令行参数
            var config = ParseArguments(args);

            // 交互式模式:提示用户输入
            if (config.IsInteractive)
            {
                config = await PromptForInputAsync(config);
            }

            // 显示配置信息
            DisplayConfiguration(config);

            // 二次确认
            if (!ConfirmOperation())
            {
                Console.WriteLine("操作已取消");
                return 0;
            }

            // 执行密码重置
            await ResetPasswordAsync(config);

            Console.WriteLine();
            Console.WriteLine("✓ 密码重置成功!");
            Console.WriteLine();
            Console.WriteLine("登录信息:");
            Console.WriteLine($"  用户名: {config.Username}");
            Console.WriteLine($"  新密码: {config.NewPassword}");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"✗ 错误: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine($"详细信息: {ex}");
            return 1;
        }
        finally
        {
            Console.WriteLine("===== 完成 =====");
        }
    }

    /// <summary>
    /// 解析命令行参数
    /// </summary>
    static ResetPasswordConfig ParseArguments(string[] args)
    {
        var config = new ResetPasswordConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--type":
                case "-t":
                    if (i + 1 < args.Length)
                    {
                        config.AccountType = args[++i].ToLower() == "sysadmin"
                            ? AccountType.SysAdmin
                            : AccountType.User;
                        config.IsInteractive = false;
                    }
                    break;

                case "--username":
                case "-u":
                    if (i + 1 < args.Length)
                    {
                        config.Username = args[++i];
                    }
                    break;

                case "--password":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        config.NewPassword = args[++i];
                    }
                    break;

                case "--connection":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        config.ConnectionString = args[++i];
                    }
                    break;

                case "--help":
                case "-h":
                    DisplayHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return config;
    }

    /// <summary>
    /// 交互式提示用户输入
    /// </summary>
    static async Task<ResetPasswordConfig> PromptForInputAsync(ResetPasswordConfig config)
    {
        // 选择账户类型
        Console.WriteLine("请选择账户类型:");
        Console.WriteLine("  1. SysAdmin (管理员账户)");
        Console.WriteLine("  2. User (普通用户)");
        Console.Write("请输入选项 (1/2): ");

        var choice = Console.ReadLine();
        config.AccountType = choice == "1" ? AccountType.SysAdmin : AccountType.User;

        // 如果是普通用户,需要输入用户名
        if (config.AccountType == AccountType.User)
        {
            Console.Write("请输入用户名: ");
            config.Username = Console.ReadLine() ?? string.Empty;
        }
        else
        {
            config.Username = "sysadmin";
        }

        // 输入新密码
        Console.Write("请输入新密码: ");
        config.NewPassword = Console.ReadLine() ?? string.Empty;

        // 确认密码
        Console.Write("请再次输入新密码: ");
        var confirmPassword = Console.ReadLine() ?? string.Empty;

        if (config.NewPassword != confirmPassword)
        {
            throw new InvalidOperationException("两次密码输入不一致!");
        }

        Console.WriteLine();

        return await Task.FromResult(config);
    }

    /// <summary>
    /// 显示配置信息
    /// </summary>
    static void DisplayConfiguration(ResetPasswordConfig config)
    {
        Console.WriteLine("操作配置:");
        Console.WriteLine($"  账户类型: {(config.AccountType == AccountType.SysAdmin ? "SysAdmin (管理员)" : "User (普通用户)")}");
        Console.WriteLine($"  用户名: {config.Username}");
        Console.WriteLine($"  新密码: {new string('*', config.NewPassword.Length)}");
        Console.WriteLine();
    }

    /// <summary>
    /// 二次确认操作
    /// </summary>
    static bool ConfirmOperation()
    {
        Console.Write("确认执行密码重置? (y/n): ");
        var confirm = Console.ReadLine()?.ToLower();
        Console.WriteLine();

        return confirm == "y" || confirm == "yes";
    }

    /// <summary>
    /// 执行密码重置
    /// </summary>
    static async Task ResetPasswordAsync(ResetPasswordConfig config)
    {
        // 生成BCrypt哈希 (workfactor=11,与AuthService一致)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(config.NewPassword, 11);
        Console.WriteLine("✓ 密码哈希已生成 (BCrypt workfactor=11)");

        // 连接数据库
        using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync();
        Console.WriteLine("✓ 数据库连接成功");

        if (config.AccountType == AccountType.SysAdmin)
        {
            await ResetSysAdminPasswordAsync(connection, passwordHash);
        }
        else
        {
            await ResetUserPasswordAsync(connection, config.Username, passwordHash);
        }
    }

    /// <summary>
    /// 重置SysAdmin密码
    /// </summary>
    static async Task ResetSysAdminPasswordAsync(SqlConnection connection, string passwordHash)
    {
        // 查询AdminSecrets表
        var queryAdmin = @"
SELECT Id, PasswordHash
FROM AdminSecrets
WHERE Id = '00000000-0000-0000-0000-000000000001'";

        using var cmdQuery = new SqlCommand(queryAdmin, connection);
        using var reader = await cmdQuery.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var adminId = reader["Id"];
            var oldHash = reader["PasswordHash"];

            Console.WriteLine("找到SysAdmin账户:");
            Console.WriteLine($"  ID: {adminId}");
            Console.WriteLine($"  旧哈希: {oldHash.ToString()!.Substring(0, 20)}...");
            Console.WriteLine();

            await reader.CloseAsync();

            // 更新密码
            var updatePassword = @"
UPDATE AdminSecrets
SET PasswordHash = @PasswordHash
WHERE Id = '00000000-0000-0000-0000-000000000001'";

            using var cmdUpdate = new SqlCommand(updatePassword, connection);
            cmdUpdate.Parameters.AddWithValue("@PasswordHash", passwordHash);

            var rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();

            if (rowsAffected <= 0)
            {
                throw new InvalidOperationException("密码更新失败");
            }

            Console.WriteLine("✓ SysAdmin密码已更新");
        }
        else
        {
            throw new InvalidOperationException("未找到SysAdmin账户");
        }
    }

    /// <summary>
    /// 重置普通用户密码
    /// </summary>
    static async Task ResetUserPasswordAsync(SqlConnection connection, string username, string passwordHash)
    {
        // 查询用户信息
        var queryUser = @"
SELECT Id, UserName, Email, RealName, Status
FROM Users
WHERE UserName = @UserName AND IsDeleted = 0";

        using var cmdQuery = new SqlCommand(queryUser, connection);
        cmdQuery.Parameters.AddWithValue("@UserName", username);

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

            if (rowsAffected <= 0)
            {
                throw new InvalidOperationException("密码更新失败");
            }

            Console.WriteLine($"✓ 用户 {username} 密码已更新");
        }
        else
        {
            throw new InvalidOperationException($"未找到用户: {username}");
        }
    }

    /// <summary>
    /// 显示帮助信息
    /// </summary>
    static void DisplayHelp()
    {
        Console.WriteLine("===== 使用说明 =====");
        Console.WriteLine();
        Console.WriteLine("交互式模式:");
        Console.WriteLine("  dotnet run --project scripts/ResetPassword/ResetPassword.csproj");
        Console.WriteLine();
        Console.WriteLine("命令行模式:");
        Console.WriteLine("  dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- [选项]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  --type, -t <类型>        账户类型: sysadmin 或 user");
        Console.WriteLine("  --username, -u <用户名>  用户名 (仅普通用户需要)");
        Console.WriteLine("  --password, -p <密码>    新密码");
        Console.WriteLine("  --connection, -c <连接>  数据库连接字符串");
        Console.WriteLine("  --help, -h               显示此帮助信息");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  # 重置SysAdmin密码");
        Console.WriteLine("  dotnet run -- -t sysadmin -p \"NewSecurePass123!\"");
        Console.WriteLine();
        Console.WriteLine("  # 重置普通用户密码");
        Console.WriteLine("  dotnet run -- -t user -u doctor1 -p \"NewPass123!\"");
        Console.WriteLine();
    }
}

/// <summary>
/// 账户类型
/// </summary>
enum AccountType
{
    SysAdmin,
    User
}

/// <summary>
/// 密码重置配置
/// </summary>
class ResetPasswordConfig
{
    public AccountType AccountType { get; set; } = AccountType.User;
    public string Username { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true";
    public bool IsInteractive { get; set; } = true;
}
