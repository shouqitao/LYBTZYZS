using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Integration.Fixtures;

/// <summary>
/// Server端集成测试共享Fixture。
/// 使用WebApplicationFactory创建完整HTTP管线，SQL Server测试数据库(LYBT_Test)。
/// 每个测试类通过IClassFixture共享同一实例。
/// </summary>
public class WebApiFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;

    /// <summary>带Admin权限的HttpClient</summary>
    public HttpClient AdminClient { get; private set; } = null!;

    /// <summary>带Doctor权限的HttpClient</summary>
    public HttpClient DoctorClient { get; private set; } = null!;

    /// <summary>无认证的HttpClient</summary>
    public HttpClient AnonymousClient { get; private set; } = null!;

    /// <summary>WebApplicationFactory的服务容器</summary>
    public IServiceProvider Services => _factory.Services;

    // 固定测试用户ID
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DoctorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // JWT配置 - 必须与WebAPI/appsettings.Test.json一致
    private const string JwtSecretKey = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2";
    private const string JwtIssuer = "LYBT.WebAPI.Tests";
    private const string JwtAudience = "LYBT.Client.Tests";

    // 测试用密码
    public const string AdminPassword = "TestAdmin2025@";
    public const string DoctorPassword = "TestDoctor2025@";

    // SQL Server测试数据库连接字符串
    private const string TestConnectionString =
        "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");

                builder.ConfigureServices(services =>
                {
                    // 替换数据库连接为SQL Server测试数据库
                    ReplaceDbContext(services);

                    // 移除长运行后台服务 (避免干扰测试)
                    RemoveHostedServices(services);
                });
            });

        // 初始化测试数据库: 删除旧数据 -> 创建Schema -> 种子用户
        await InitializeDatabase();

        // 创建预配置的HttpClient
        AdminClient = CreateAuthenticatedClient(UserRole.Admin, AdminUserId, "admin");
        DoctorClient = CreateAuthenticatedClient(UserRole.Doctor, DoctorUserId, "doctor");
        AnonymousClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AdminClient?.Dispose();
        DoctorClient?.Dispose();
        AnonymousClient?.Dispose();

        // 清理测试数据库
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();

        await _factory.DisposeAsync();
    }

    /// <summary>创建裸HttpClient (无认证头)</summary>
    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>创建指定角色的认证HttpClient</summary>
    public HttpClient CreateClientAs(UserRole role, Guid userId, string username = "testuser")
    {
        return CreateAuthenticatedClient(role, userId, username);
    }

    /// <summary>通过DbContext直接种子数据</summary>
    public async Task<T> SeedAsync<T>(Func<AppDbContext, Task<T>> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await seedAction(db);
    }

    /// <summary>通过DbContext直接种子数据(无返回值)</summary>
    public async Task SeedAsync(Func<AppDbContext, Task> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seedAction(db);
    }

    /// <summary>获取Service实例(创建新Scope)</summary>
    public T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    #region Private Setup

    private static void ReplaceDbContext(IServiceCollection services)
    {
        // 移除原有DbContext配置
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(TestConnectionString);
        });
    }

    private static void RemoveHostedServices(IServiceCollection services)
    {
        var hostedServices = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();
        foreach (var svc in hostedServices)
            services.Remove(svc);
    }

    private async Task InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        // 使用MigrateAsync而非EnsureCreated，确保迁移中的唯一索引等约束生效
        await db.Database.MigrateAsync();

        await SeedDefaultUsers(db);
    }

    private HttpClient CreateAuthenticatedClient(UserRole role, Guid userId, string username)
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(role, userId, username);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// 生成与应用程序JWT中间件兼容的Token。
    /// 密钥编码方式: Encoding.UTF8.GetBytes (与JwtService.cs:87一致)
    /// Claims结构与JwtService.GenerateToken一致。
    /// </summary>
    private static string GenerateJwtToken(UserRole role, Guid userId, string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("user_type", "user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 种子默认测试用户 (Upsert模式)。
    /// 迁移可能已种子部分用户，需要处理PK冲突。
    /// </summary>
    private static async Task SeedDefaultUsers(AppDbContext db)
    {
        await UpsertUser(db, AdminUserId, "admin", "系统管理员", UserRole.Admin, AdminPassword);
        await UpsertUser(db, DoctorUserId, "doctor", "测试医生", UserRole.Doctor, DoctorPassword);
        await db.SaveChangesAsync();
    }

    private static async Task UpsertUser(
        AppDbContext db, Guid id, string userName, string realName,
        UserRole role, string password)
    {
        var existing = await db.Set<User>().FindAsync(id);
        if (existing != null)
        {
            // 迁移已种子 - 更新为测试预期状态
            existing.UserName = userName;
            existing.RealName = realName;
            existing.Role = role;
            existing.Status = CommonStatus.Enabled;
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.Set<User>().Add(new User
            {
                Id = id,
                UserName = userName,
                RealName = realName,
                Role = role,
                Status = CommonStatus.Enabled,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    #endregion
}
