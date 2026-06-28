# Sysadmin 独立用户 + 用户管理重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute.

**Goal:** 将 Sysadmin 从角色模式迁移到独立用户模式，并完成用户管理模块的全面稳固化。

**Architecture:** Sysadmin 改为 ApplicationUser 的 IsSysAdmin 布尔字段，删除 SuperAdminRoleDefinition。保留 2 条授权策略（DoctorOrReceptionist + AdminOrSuperAdmin）。多层密码保护：PBKDF2 + HMAC + DPAPI。

**Tech Stack:** C# / .NET 8 / ASP.NET Core Identity / EF Core

---

## Task 1: ApplicationUser 增加 IsSysAdmin 字段

**Files:**
- Modify: `src/Server/Core/LYBT.Entities/Users/ApplicationUser.cs`
- Create: EF Migration

- [ ] **Step 1: 添加字段**

```csharp
/// <summary>
/// 标识此用户为系统管理员（独立于角色体系）。
/// sysadmin 在安装时自动创建，不可删除/禁用。
/// </summary>
[DisplayName("系统管理员")]
public bool IsSysAdmin { get; set; } = false;
```

- [ ] **Step 2: EF Migration**

```bash
dotnet ef migrations add AddIsSysAdmin --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

在 Migration 的 `Up` 方法中添加：
```csharp
migrationBuilder.AddColumn<bool>(
    name: "IsSysAdmin",
    table: "AspNetUsers",
    type: "bit",
    nullable: false,
    defaultValue: false);
```

在种子数据中，将 sysadmin 用户的 `IsSysAdmin` 设为 `true`。

- [ ] **Step 3: 构建验证**

---

## Task 2: 删除 SuperAdminRoleDefinition

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/SuperAdminRoleDefinition.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs` — 移除注册
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — 如有 SuperAdmin 相关常量则移除

- [ ] **Step 1: 删除文件**
- [ ] **Step 2: 从 DataSourceRegistrationExtensions.cs 移除**
```csharp
// 删除这行：
registry.Register(new SuperAdminRoleDefinition());
```
- [ ] **Step 3: 构建验证**

---

## Task 3: 修改 CanManageUser — sysadmin 绕过角色检查

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`

- [ ] **Step 1: CanManageUser 方法增加 sysadmin 特判**

```csharp
private bool CanManageUser(ApplicationUser currentUser, UserRole? targetRole)
{
    // Sysadmin 可管理所有角色
    if (currentUser.IsSysAdmin)
        return true;
    // ... 原有角色层级逻辑不变
}
```

同时需要在获取 `currentUser` 的地方，通过 `UserManager` 查找并加载 `IsSysAdmin` 属性。

- [ ] **Step 2: Delete 端点增加 sysadmin 保护**

```csharp
// sysadmin 不可被删除
if (user.IsSysAdmin)
    return Forbid("系统管理员账号不可被删除");
```

- [ ] **Step 3: 构建验证**

---

## Task 4: AuthController 登录流程适配

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`

- [ ] **Step 1: 登录成功时设置 IsSysAdmin Claims**

在生成 JWT token 时，添加 `IsSysAdmin` claim：
```csharp
if (user.IsSysAdmin)
{
    claims.Add(new Claim("IsSysAdmin", "true"));
}
```

- [ ] **Step 2: Validate 端点返回 IsSysAdmin 信息**

- [ ] **Step 3: 构建验证**

---

## Task 5: 种子数据 — 创建 sysadmin + admin 两个独立用户

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Data/LocalWebApiSeedData.cs`

> ⚠️ sysadmin 和 admin 是**两个独立用户**，不是同一用户。sysadmin 是信任根，创建 admin，可重置 admin 密码。

- [ ] **Step 1: 修改 IdentitySeedData — 创建两个用户**

```csharp
// 1. 创建 sysadmin（系统运维）
var sysadmin = await userManager.FindByNameAsync("sysadmin");
if (sysadmin == null)
{
    sysadmin = new ApplicationUser
    {
        UserName = "sysadmin",
        RealName = "系统运维",
        Email = "sysadmin@lybtzyzs.local",
        IsSysAdmin = true
    };
    await userManager.CreateAsync(sysadmin, "SysAdmin@2026!");
    await userManager.AddToRoleAsync(sysadmin, "SuperAdmin");
}

// 2. 创建 admin（业务管理员）
var admin = await userManager.FindByNameAsync("admin");
if (admin == null)
{
    admin = new ApplicationUser
    {
        UserName = "admin",
        RealName = "系统管理员",
        Email = "admin@lybtzyzs.local",
        IsSysAdmin = false
    };
    await userManager.CreateAsync(admin, "Admin@123456");
    await userManager.AddToRoleAsync(admin, "Admin");
}
```

- [ ] **Step 2: 修改 LocalWebApiSeedData — 同步创建两个用户**

更新本地种子数据，使用 Identity UserManager 而非旧 PasswordHelper。

- [ ] **Step 3: 构建验证

---

## Task 6: 最终验证

- [ ] **Step 1: 全量构建**
```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 2: 运行测试**
```bash
dotnet test tests/LYBT.Tests.Server/ --filter "User|Auth"
```

- [ ] **Step 3: EF Migration 应用**
```bash
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

- [ ] **Step 4: Commit + Push**
