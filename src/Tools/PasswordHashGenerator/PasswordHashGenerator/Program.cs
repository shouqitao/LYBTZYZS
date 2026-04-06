using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;

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
                string? password = GetDefaultAdminPassword();
                if (password == null)
                    return 1;
                UserRole role = UserRole.Doctor;
                bool showConfig = false;

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
                        case "--show-config":
                            showConfig = true;
                            break;
                        case "--show-help":
                        case "-h":
                            ShowHelp();
                            return 0;
                    }
                }

                // 获取当前时间
                var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"执行时间: {currentTime}");
                Console.WriteLine();

                // 显示配置信息
                if (showConfig)
                {
                    ShowConfiguration();
                    Console.WriteLine();
                }

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
            Console.WriteLine("  --role <角色>         用户角色（默认：Doctor）");
            Console.WriteLine("  --show-config         显示当前密码帮助类配置");
            Console.WriteLine("  --show-help, -h       显示此帮助信息");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  PasswordHashGenerator");
            Console.WriteLine("  PasswordHashGenerator --password \"MyNewPassword123\" --role Admin");
            Console.WriteLine("  PasswordHashGenerator --show-config");
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

        static void ShowConfiguration()
        {
            Console.WriteLine("🔧 当前密码帮助类配置:");
            var config = PasswordHelper.GetConfiguration();
            
            Console.WriteLine($"   工作因子: {config.WorkFactor}");
            Console.WriteLine($"   启用重新哈希: {(config.EnableRehashing ? "是" : "否")}");
            Console.WriteLine($"   密码历史记录数量: {config.PasswordHistoryCount}");
            Console.WriteLine($"   默认工作因子: {config.DefaultWorkFactor}");
            Console.WriteLine($"   最小工作因子: {config.MinWorkFactor}");
            Console.WriteLine($"   最大工作因子: {config.MaxWorkFactor}");
        }

        static void GeneratePasswordHash(string password, UserRole role)
        {
            Console.WriteLine($"🔐 密码哈希生成开始...");
            Console.WriteLine($"   用户角色: {role}");
            Console.WriteLine($"   密码长度: {password.Length} 字符");
            Console.WriteLine($"   使用BCrypt算法，工作因子: {PasswordHelper.WorkFactor}");
            Console.WriteLine();

            try
            {
                // 使用统一的PasswordHelper生成哈希
                var hashedPassword = PasswordHelper.HashPassword(password, role);
                
                Console.WriteLine("✅ 密码哈希生成成功！");
                Console.WriteLine();
                
                // 输出结果
                Console.WriteLine("📋 哈希结果:");
                Console.WriteLine($"   原始密码: {password}");
                Console.WriteLine($"   哈希密码: {hashedPassword}");
                Console.WriteLine();
                
                Console.WriteLine("💡 SQL更新语句:");
                Console.WriteLine($"   UPDATE Users SET PasswordHash = '{hashedPassword}' WHERE UserName = '你的用户名';");
                Console.WriteLine();
                
                Console.WriteLine("🔍 验证命令:");
                Console.WriteLine($"   dotnet run --project PasswordHashGenerator --password \"{password}\" --role {role}");
                Console.WriteLine();

                // 验证哈希
                Console.WriteLine("🧪 验证哈希结果...");
                var verificationResult = PasswordHelper.VerifyPassword(password, hashedPassword, role);
                
                if (verificationResult.IsSuccess)
                {
                    Console.WriteLine("✅ 验证成功 - 哈希值正确");
                    if (verificationResult.NeedsRehash)
                    {
                        Console.WriteLine("⚠️  检测到需要重新哈希");
                    }
                }
                else
                {
                    Console.WriteLine("❌ 验证失败 - 这不应该发生！");
                    Console.WriteLine($"   错误信息: {verificationResult.ErrorMessage}");
                }

                // 显示哈希详细信息
                Console.WriteLine();
                Console.WriteLine("🔐 哈希详细信息:");
                Console.WriteLine($"   算法: BCrypt");
                Console.WriteLine($"   工作因子: {PasswordHelper.WorkFactor}");
                Console.WriteLine($"   哈希前缀: {(hashedPassword.StartsWith("$2a$") ? "✅ BCrypt格式正确" : "❌ 格式异常")}");
                Console.WriteLine($"   验证时间: {verificationResult.Timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
                
                // 临时密码生成演示
                Console.WriteLine("🔑 临时密码生成示例:");
                var tempPassword = PasswordHelper.GenerateTemporaryPassword();
                Console.WriteLine($"   生成临时密码: {tempPassword}");
                var tempHash = PasswordHelper.HashPassword(tempPassword, role);
                Console.WriteLine($"   临时密码哈希: {tempHash}");

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