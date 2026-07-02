# Users Infrastructure Layer Implementation Plan

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan task-by-task.

**Goal:** Create Infrastructure layer for Users module with UsersDbContext, UserRepository, and DI registration.

**Architecture:** Module-scoped DbContext (shared connection, separate schema), UserRepository implementing IUserRepository, updated UsersModule for DI registration.

**Tech Stack:** .NET 8, EF Core 8, Identity, FluentValidation

## Global Constraints

- .NET 8.0, LangVersion=latest, Nullable=enable
- All public types must have XML documentation
- Use existing AppDbContext patterns for entity configuration
- UserRepository extends BaseRepository pattern

---

### Task 1: Create UsersDbContext

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Infrastructure/UsersDbContext.cs`

**Interfaces:**
- Consumes: Domain entities
- Produces: Module-scoped DbContext

- [ ] **Step 1: Create UsersDbContext**

```csharp
// src/Server/Modules/LYBT.Module.Users/Infrastructure/UsersDbContext.cs
using LYBT.Module.Users.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Infrastructure;

/// <summary>
/// 用户模块数据库上下文。使用共享连接、独立Schema的方式实现模块数据隔离。
/// </summary>
public class UsersDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>认证会话</summary>
    public DbSet<AuthSession> AuthSessions { get; set; } = null!;

    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 设置用户模块Schema
        modelBuilder.HasDefaultSchema("Users");

        // 应用程序用户配置
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.UserName).HasMaxLength(32).IsRequired();
            entity.Property(e => e.RealName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PinYinCode).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);
            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();

            // 索引
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.PinYinCode);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.Status);
        });

        // 认证会话配置
        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiryTime);
        });
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Infrastructure/
git commit -m "feat(users-infrastructure): add UsersDbContext with Identity integration"
```

---

### Task 2: Create UserRepository

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Infrastructure/UserRepository.cs`

**Interfaces:**
- Consumes: `IUserRepository` (from Domain layer), `UsersDbContext`
- Produces: Repository implementation

- [ ] **Step 1: Create UserRepository**

```csharp
// src/Server/Modules/LYBT.Module.Users/Infrastructure/UserRepository.cs
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Infrastructure;

/// <summary>
/// 用户仓储实现。封装用户数据访问逻辑。
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ApplicationUser>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        UserRole? role, CommonStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(u =>
                u.UserName.ToLower().Contains(kw) ||
                u.RealName.ToLower().Contains(kw) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)) ||
                (u.Email != null && u.Email.ToLower().Contains(kw)));
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationUser>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.UserName == userName && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Infrastructure/UserRepository.cs
git commit -m "feat(users-infrastructure): add UserRepository implementation"
```

---

### Task 3: Update UsersModule DI Registration

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`

**Interfaces:**
- Consumes: `UsersDbContext`, `UserRepository`, `IUserRepository`
- Produces: Updated DI registration

- [ ] **Step 1: Update UsersModule**

```csharp
// src/Server/Modules/LYBT.Module.Users/UsersModule.cs
using FluentValidation;
using LYBT.Module.Users.Application.Commands;
using LYBT.Module.Users.Application.Validators;
using LYBT.Module.Users.Infrastructure;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Users;

/// <summary>
/// 用户模块注册
/// </summary>
public static class UsersModule
{
    /// <summary>
    /// 注册用户模块服务
    /// </summary>
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册 DbContext
        services.AddDbContext<UsersDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // 注册 Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<UsersDbContext>()
            .AddDefaultTokenProviders();

        // 注册仓储
        services.AddScoped<IUserRepository, UserRepository>();

        // 注册领域服务
        services.AddScoped<IUserManagerService, UserManagerService>();

        // 注册应用服务
        services.AddScoped<IUserService, UserService>();

        // 注册 MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateUserCommand).Assembly));

        // 注册 FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

        // 注册跨模块服务
        services.AddScoped<IUserCrossModuleService, UserCrossModuleService>();

        return services;
    }

    /// <summary>
    /// 配置用户模块中间件
    /// </summary>
    public static IApplicationBuilder UseUsersModule(this IApplicationBuilder app)
    {
        return app;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/UsersModule.cs
git commit -m "feat(users-infrastructure): update UsersModule with DbContext and MediatR registration"
```

---

### Task 4: Full Solution Build Verification

**Files:**
- None (verification only)

- [ ] **Step 1: Build full solution**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: Build succeeded (0 errors)

- [ ] **Step 2: Verify no new errors**

Expected: Only pre-existing warnings

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "fix(users-infrastructure): address build issues"
```
