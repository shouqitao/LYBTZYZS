# WebAPI 重构实施计划（阶段 1：Identity 集成）

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Users 模块替换为 ASP.NET Core Identity，保持双模式兼容和 WPF 前端不变。

**Architecture:** ApplicationUser 继承 IdentityUser，UserManager 替代自定义 UserService，JwtService 保留。远程模式用完整 Identity，本地模式保持简化认证。

**Tech Stack:** ASP.NET Core Identity 8, EF Core, JWT (自定义), WPF/Prism

---

## 文件结构

| 文件 | 操作 | 职责 |
|------|------|------|
| `src/Server/Core/LYBT.Entities/Users/ApplicationUser.cs` | 新建 | Identity User 实体 |
| `src/Server/Core/LYBT.Entities/Users/ApplicationRole.cs` | 新建 | Identity Role 实体 |
| `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` | 修改 | 继承 IdentityDbContext |
| `src/Server/Core/LYBT.Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs` | 新建 | EF 配置 |
| `src/Server/Modules/LYBT.Module.Users/Services/UserManagerService.cs` | 新建 | 包装 UserManager 的业务服务 |
| `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserManagerService.cs` | 新建 | 接口 |
| `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs` | 修改 | 改用 UserManagerService |
| `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs` | 修改 | 改用 SignInManager |
| `src/Server/Services/LYBT.WebAPI/Program.cs` | 修改 | 注册 Identity 服务 |
| `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | 修改 | 注册 Identity 服务 |
| `src/Client/Desktop/Shell/ViewModels/LoginViewModel.cs` | 修改 | 适配 Identity 登录 |
| `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs` | 修改 | 适配 Identity DTO |
| `src/Server/Core/LYBT.Infrastructure/Data/Migrations/` | 新建 | EF Migration |
| `tests/LYBT.Tests.Server/Integration/Users/` | 修改 | 更新测试 |

## Task 1: 创建 ApplicationUser 和 ApplicationRole 实体

**Covers:** [S5]

**Files:**
- Create: `src/Server/Core/LYBT.Entities/Users/ApplicationUser.cs`
- Create: `src/Server/Core/LYBT.Entities/Users/ApplicationRole.cs`

- [ ] **Step 1: 创建 ApplicationUser**

```csharp
using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
```

- [ ] **Step 2: 创建 ApplicationRole**

```csharp
using Microsoft.AspNetCore.Identity;

namespace LYBT.Entities.Users;

public class ApplicationRole : IdentityRole<Guid>
{
    /// <summary>
    /// 角色描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
```

- [ ] **Step 3: 更新 LYBT.Entities.csproj 添加 Identity 包引用**

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
```

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Entities/
git commit -m "feat(users): add ApplicationUser and ApplicationRole entities"
```

---

## Task 2: 修改 AppDbContext 继承 IdentityDbContext

**Covers:** [S5]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/ApplicationUserConfiguration.cs`

- [ ] **Step 1: 修改 AppDbContext 继承**

```csharp
// 旧：public class AppDbContext : DbContext
// 新：
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    // 保留现有 DbSet 属性...
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity 表配置
        
        // 应用现有实体配置...
    }
}
```

- [ ] **Step 2: 创建 ApplicationUserConfiguration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LYBT.Entities.Users;

namespace LYBT.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.RealName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);
    }
}
```

- [ ] **Step 3: 更新 LYBT.Infrastructure.csproj 添加 Identity 包引用**

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
```

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/
git commit -m "feat(users): AppDbContext inherits IdentityDbContext"
```

---

## Task 3: 创建 UserManagerService 包装层

**Covers:** [S2]

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserManagerService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserManagerService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Repositories/AppUserRepository.cs`

- [ ] **Step 1: 创建 IUserManagerService 接口**

```csharp
namespace LYBT.Module.Users.Interfaces;

public interface IUserManagerService
{
    Task<ApplicationUser?> FindByNameAsync(string userName);
    Task<ApplicationUser?> FindByIdAsync(Guid id);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
    Task<IdentityResult> UpdateAsync(ApplicationUser user);
    Task<IdentityResult> DeleteAsync(ApplicationUser user);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role);
    Task<bool> IsInRoleAsync(ApplicationUser user, string role);
    Task<IQueryable<ApplicationUser>> GetUsersAsync();
    Task<int> GetUsersCountAsync();
}
```

- [ ] **Step 2: 创建 UserManagerService 实现**

```csharp
using Microsoft.AspNetCore.Identity;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Services;

public class UserManagerService : IUserManagerService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagerService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<ApplicationUser?> FindByNameAsync(string userName)
        => _userManager.FindByNameAsync(userName);

    public Task<ApplicationUser?> FindByIdAsync(Guid id)
        => _userManager.FindByIdAsync(id.ToString());

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
        => _userManager.GetRolesAsync(user);

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        => _userManager.CreateAsync(user, password);

    public Task<IdentityResult> UpdateAsync(ApplicationUser user)
        => _userManager.UpdateAsync(user);

    public Task<IdentityResult> DeleteAsync(ApplicationUser user)
        => _userManager.DeleteAsync(user);

    public Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
        => _userManager.AddToRoleAsync(user, role);

    public Task<IdentityResult> RemoveFromRoleAsync(ApplicationUser user, string role)
        => _userManager.RemoveFromRoleAsync(user, role);

    public Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        => _userManager.IsInRoleAsync(user, role);

    public Task<IQueryable<ApplicationUser>> GetUsersAsync()
        => Task.FromResult(_userManager.Users.AsQueryable());

    public Task<int> GetUsersCountAsync()
        => _userManager.Users.CountAsync();
}
```

- [ ] **Step 3: 创建种子数据（4 个角色 + 默认 Admin）**

```csharp
// 在 AuthModule 或 UsersModule 中
public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Receptionist", "Doctor", "Admin", "SuperAdmin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    // 创建默认 Admin
    var admin = await userManager.FindByNameAsync("admin");
    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = "admin",
            RealName = "系统管理员",
            Email = "admin@lybtzyzs.local"
        };
        await userManager.CreateAsync(admin, "Admin@123456");
        await userManager.AddToRoleAsync(admin, "SuperAdmin");
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/
git commit -m "feat(users): add UserManagerService wrapping Identity"
```

---

## Task 4: 注册 Identity 服务（WebAPI + LocalWebAPI）

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Program.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs`

- [ ] **Step 1: WebAPI 注册 Identity**

```csharp
// 在 Program.cs 中添加
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

- [ ] **Step 2: LocalWebAPI 注册 Identity（简化版）**

```csharp
// 在 LocalWebApiProgram.cs 中添加
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // 本地模式：简化密码策略
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    // 本地模式：不锁定
    options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
    options.Lockout.AllowedForNewUsers = false;
    options.Lockout.EnabledForNewUsers = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

- [ ] **Step 3: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Program.cs
git add src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs
git commit -m "feat(users): register Identity services in WebAPI and LocalWebAPI"
```

---

## Task 5: 更新 AuthController 使用 SignInManager

**Covers:** [S2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/LocalWebAPI/Controllers/AuthController.cs`

- [ ] **Step 1: Server AuthController 登录逻辑**

```csharp
// 旧：IAuthService.VerifyCredentialsAsync
// 新：
private readonly SignInManager<ApplicationUser> _signInManager;

[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _userManager.FindByNameAsync(request.UserName);
    if (user == null)
        return Unauthorized(new { message = "用户名或密码错误" });

    var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
    if (!result.Succeeded)
        return Unauthorized(new { message = "用户名或密码错误" });

    // 更新最后登录时间
    user.LastLoginAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // 生成 JWT
    var roles = await _userManager.GetRolesAsync(user);
    var token = _jwtService.GenerateToken(user.Id, user.UserName, roles);

    return Ok(new { token, user.Id, user.UserName, roles });
}
```

- [ ] **Step 2: LocalWebAPI AuthController（保持简化）**

本地模式的 AuthController 可以保持现有的简化逻辑（直接密码验证 + 简化 JWT），或者也改用 SignInManager。建议改用 SignInManager 以保持一致性。

- [ ] **Step 3: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/
git commit -m "feat(auth): update AuthController to use SignInManager"
```

---

## Task 6: 更新 UsersController 使用 UserManagerService

**Covers:** [S2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`

- [ ] **Step 1: 替换 IUserService → IUserManagerService**

将所有 `_userService` 调用替换为 `_userManagerService` 调用。主要变更：

```csharp
// 创建用户：现在需要密码
[HttpPost]
public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
{
    var user = new ApplicationUser
    {
        UserName = request.UserName,
        RealName = request.RealName,
        Email = request.Email
    };
    var result = await _userManagerService.CreateAsync(user, request.Password);
    if (!result.Succeeded)
        return BadRequest(result.Errors);
    await _userManagerService.AddToRoleAsync(user, request.Role);
    return Ok();
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs
git commit -m "feat(users): update UsersController to use UserManagerService"
```

---

## Task 7: 更新测试

**Covers:** [S5]

**Files:**
- Modify: `tests/LYBT.Tests.Server/Integration/Users/` (所有测试文件)

- [ ] **Step 1: 更新测试以使用 Identity**

更新测试中的用户创建、密码验证等逻辑，使用 UserManagerService 而非旧的 UserService。

- [ ] **Step 2: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "Category=Users"
```

- [ ] **Step 3: Commit**

```bash
git add tests/LYBT.Tests.Server/Integration/Users/
git commit -m "test(users): update tests for Identity integration"
```

---

## Task 8: 生成 EF Migration

**Covers:** [S5]

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Data/Migrations/`

- [ ] **Step 1: 生成 Migration**

```bash
dotnet ef migrations add AddIdentityTables --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

- [ ] **Step 2: 验证 Migration 文件**

检查生成的 Migration 文件，确认包含：
- AspNetUsers 表（替换旧 Users 表）
- AspNetRoles 表（4 个种子角色）
- AspNetUserRoles 关联表
- 其他 Identity 标准表

- [ ] **Step 3: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Data/Migrations/
git commit -m "feat(users): add Identity EF migration"
```

---

## Task 9: 全量验证

**Covers:** [S5]

- [ ] **Step 1: 编译验证**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 2: 运行全部测试**

```bash
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```

- [ ] **Step 3: 更新文档**

更新 `docs/02-requirements/03-users.md` 和 `docs/02-requirements/02-auth.md` 中的用户管理相关需求。

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat(users): complete Identity integration"
```

---

## 执行顺序

| Task | 依赖 | 预估时间 |
|------|------|---------|
| Task 1 | 无 | 15 min |
| Task 2 | Task 1 | 30 min |
| Task 3 | Task 2 | 45 min |
| Task 4 | Task 2 | 20 min |
| Task 5 | Task 3, 4 | 30 min |
| Task 6 | Task 3 | 30 min |
| Task 7 | Task 5, 6 | 45 min |
| Task 8 | Task 2 | 15 min |
| Task 9 | 全部 | 30 min |

**总计：~4 小时**（Phase 1）
