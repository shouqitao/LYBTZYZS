using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;

namespace PasswordHashGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("凌隐宝堂 - 密码哈希生成工具");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            try
            {
                // 解析命令行参数
                string? password = null;
                UserRole role = UserRole.Doctor;

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "--password":
                            if (i + 1 < args.Length)
                                password = args[++i];
                            break;
                        case "--role":
                            if (i + 1 < args.Length)
                            {
                                var roleStr = args[++i];
                                if (Enum.TryParse<UserRole>(roleStr, true, out var parsedRole))
                                    role = parsedRole;
                            }
                            break;
                        case "--show-help":
                        case "-h":
                            ShowHelp();
                            return 0;
                    }
                }

                // 未提供 --password 时回退到默认管理员密码
                password ??= GetDefaultAdminPassword();
                if (password == null)
                    return 1;

                // 获取当前时间
                var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"执行时间: {currentTime}");
                Console.WriteLine();

                // 生成密码哈希
                GeneratePasswordHash(password, role);

                Console.WriteLine();
                Console.WriteLine("===========================================");
                Console.WriteLine("操作完成！");
                Console.WriteLine("===========================================");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                return 1;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("用法: PasswordHashGenerator [选项]");
            Console.WriteLine();
            Console.WriteLine("选项:");
            Console.WriteLine("  --password <密码>     要哈希的密码（可选，不提供则使用默认管理员密码）");
            Console.WriteLine("  --role <角色>         用户角色（默认：Doctor，仅用于展示，不影响哈希值）");
            Console.WriteLine("  --show-help, -h       显示此帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  PasswordHashGenerator");
            Console.WriteLine("  PasswordHashGenerator --password \"MyNewPassword123\" --role Admin");
        }

        static string? GetDefaultAdminPassword()
        {
            try
            {
                // 尝试从appsettings.json读取默认密码
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                
                var configuration = configBuilder.Build();
                var defaultPassword = configuration["Lybt:DefaultPasswords:SysAdminPassword"];
                
                if (!string.IsNullOrEmpty(defaultPassword))
                {
                    Console.WriteLine($"📋 从配置读取到默认管理员密码");
                    return defaultPassword;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  无法读取配置文件: {ex.Message}");
            }

            Console.WriteLine("❌ 错误: 未找到默认管理员密码。请在 appsettings.json 中配置 Lybt:DefaultPasswords:SysAdminPassword，或通过 --password 参数提供密码。");
            return null;
        }

        static void GeneratePasswordHash(string password, UserRole role)
        {
            Console.WriteLine($"🔐 密码哈希生成开始...");
            Console.WriteLine($"   用户角色: {role}");
            Console.WriteLine($"   密码长度: {password.Length} 字符");
            Console.WriteLine($"   算法: ASP.NET Core Identity PasswordHasher (PBKDF2)");
            Console.WriteLine();

            try
            {
                // 必须使用 Identity 的 PasswordHasher（与 UserManager 登录认证同款，PBKDF2，AQAAAA 前缀）。
                // 不能用 BCrypt（PasswordHelper）：直接写入 AspNetUsers.PasswordHash 将导致该用户无法登录
                // （见 DatabaseInitializationService.cs 关于避免 BCrypt/PBKDF2 哈希冲突的说明）。
                var hasher = new PasswordHasher<ApplicationUser>();
                var user = new ApplicationUser();
                var hashedPassword = hasher.HashPassword(user, password);
                
                Console.WriteLine("✅ 密码哈希生成成功！");
                Console.WriteLine();
                
                // 输出结果
                Console.WriteLine("📋 哈希结果:");
                Console.WriteLine($"   原始密码: {password}");
                Console.WriteLine($"   哈希密码: {hashedPassword}");
                Console.WriteLine();
                
                Console.WriteLine("💡 SQL更新语句:");
                Console.WriteLine($"   UPDATE AspNetUsers SET PasswordHash = '{hashedPassword}' WHERE UserName = '你的用户名';");
                Console.WriteLine();
                
                Console.WriteLine("🔍 验证命令:");
                Console.WriteLine($"   dotnet run --project PasswordHashGenerator --password \"{password}\" --role {role}");
                Console.WriteLine();

                // 验证哈希
                Console.WriteLine("🧪 验证哈希结果...");
                var verificationResult = hasher.VerifyHashedPassword(user, hashedPassword, password);
                
                if (verificationResult != PasswordVerificationResult.Failed)
                {
                    Console.WriteLine("✅ 验证成功 - 哈希值正确");
                    if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        Console.WriteLine("⚠️  检测到需要重新哈希");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 验证失败 - 这不应该发生！");
                }

                // 显示哈希详细信息
                Console.WriteLine();
                Console.WriteLine("🔐 哈希详细信息:");
                Console.WriteLine($"   算法: ASP.NET Core Identity PBKDF2");
                Console.WriteLine($"   哈希前缀: {(hashedPassword.StartsWith("AQAAAA") ? "✅ Identity格式正确" : "❌ 格式异常")}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 密码哈希生成失败: {ex.Message}");
                Console.WriteLine($"   详细错误: {ex}");
                throw;
            }
        }
    }
}
