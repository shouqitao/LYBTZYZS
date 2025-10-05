using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// 测试种子数据生成器
/// </summary>
public class TestDataSeeder
{
    private readonly CustomWebApplicationFactory _factory;

    public TestDataSeeder(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 初始化默认测试数据
    /// </summary>
    /// <remarks>
    /// 创建基础测试账户：
    /// - admin (系统管理员)
    /// - doctor (医生)
    /// - pharmacist (药房管理员)
    /// </remarks>
    public async Task SeedDefaultUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 检查是否已有默认用户
        if (db.Users.Any(u => u.UserName == "admin"))
        {
            return; // 已初始化，跳过
        }

        var defaultUsers = new List<User>
        {
            // 系统管理员
            new User
            {
                UserName = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                RealName = "系统管理员",
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // 医生账户
            new User
            {
                UserName = "doctor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                RealName = "测试医生",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            // 药房管理员
            new User
            {
                UserName = "pharmacist",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pharmacist123!"),
                RealName = "测试药师",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        db.Users.AddRange(defaultUsers);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 清理所有测试数据
    /// </summary>
    public async Task CleanAllDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 清理所有用户数据
        var allUsers = db.Users.ToList();
        db.Users.RemoveRange(allUsers);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 重置测试数据（清理后重新初始化）
    /// </summary>
    public async Task ResetAsync()
    {
        await CleanAllDataAsync();
        await SeedDefaultUsersAsync();
    }
}
