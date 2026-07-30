---
feature: architecture-cleanup-phase1
status: draft
updated: 2026-07-30
scope: 全解决方案 CRITICAL 正确性修复（5 CRITICAL + 1 HIGH）
---

# Architecture Cleanup — Phase 1: Critical Correctness Fixes

## [S1] Problem

2026-07-30 全解决方案多维度审计发现 5 个 CRITICAL 级正确性问题和 1 个 HIGH 级安全缺陷，均为功能级或数据级错误，不修复将导致用户数据损坏或功能不可用：

| # | 问题 | 严重度 | 影响 |
|---|------|--------|------|
| C1 | 密码哈希双轨：改密/重置写 BCrypt，登录验 PBKDF2 | CRITICAL | 改密后用户锁号（线上事故级） |
| C2 | Users 表名模型↔迁移漂移：模型映射 "Users"，迁移建 "AspNetUsers" | CRITICAL | 新迁移产生错误 diff，迁移链不可维护 |
| C3 | Local EnsureCreated → 本地库永不可迁移 | CRITICAL | 本地模式无法接收后续迁移修复 |
| C4 | PrintingModule 未注册 → 处方打印静默失效 | CRITICAL | 打印功能完全不可用（被 nullable 默认值掩盖） |
| C5 | Desktop→Entities 跨层引用 + 架构测试护栏失效 | CRITICAL | 违反分层约束，护栏形同虚设 |
| H1 | 环境判定 fail-open：缺省视为 Development 绕过生产门控 | HIGH | 生产环境可能跳过数据库初始化检查 |

### 证据索引

| # | 文件:行号 | 证据 |
|---|-----------|------|
| C1 | `ChangePasswordCommandHandler.cs:32` | `PasswordHelper.HashPassword(request.NewPassword)` — 直接写 BCrypt |
| C1 | `ResetPasswordCommandHandler.cs:25` | `PasswordHelper.HashPassword(newPassword)` — 同上 |
| C1 | `UserRepository.cs:96` | `_context.Users.Update(user)` — 绕过 UserManager，BCrypt 哈希直接入库 |
| C2 | `UserConfiguration.cs:16` | `builder.ToTable("Users")` — 模型映射表名 |
| C2 | `AddIdentityTables.Designer.cs:819` | `ToTable("AspNetUsers")` — 迁移链建表名 |
| C3 | `LocalWebApiProgram.cs:127` | `await dbContext.Database.EnsureCreatedAsync()` — 非 Migrate |
| C4 | `App.xaml.cs` ConfigureModuleCatalog | 未 `AddModule<PrintingModule>()` |
| C5 | `Desktop.Infrastructure.csproj:81` | `ProjectReference Include="..\..\Server\Core\LYBT.Entities\..."` |
| C5 | `DesktopLayerArchTests.cs:35` | `NotHaveDependencyOnAll` — 应为 `NotHaveDependencyOnAny` |
| H1 | `DatabaseInitializationService.cs:211` | 环境变量缺失 → Development（fail-open） |

## [S2] Fix Design

### [S2.1] C1 — 密码哈希统一走 UserManager

**根因**：ChangePasswordCommandHandler 和 ResetPasswordCommandHandler 直接用 `PasswordHelper.HashPassword()`（BCrypt）写哈希，经 `UserRepository.UpdateAsync()` 绕过 UserManager 直接入库。而登录走 `UserManager.CheckPasswordAsync()`（PBKDF2）。两种哈希器不兼容。

**修复**：

**ChangePasswordCommandHandler** — 注入 `UserManager<ApplicationUser>`，改用：
```csharp
var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
```
此方法内部完成：验证旧密码 → 生成 PBKDF2 哈希 → 保存。不再需要手动调用 `PasswordHelper.VerifyPassword` 和 `HashPassword`。

**ResetPasswordCommandHandler** — 注入 `UserManager<ApplicationUser>`，改用：
```csharp
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
```
`GenerateSecurePassword()` 保留（用于生成临时密码返回给管理员），但哈希由 UserManager 处理。

**影响范围**：仅两个 Handler 文件。`PasswordHelper` 类保留（其他地方可能引用），这两个 Handler 不再使用它。

**验证**：`dotnet build` 通过；`dotnet test tests/LYBT.Tests.Server/ --filter "ChangePassword|ResetPassword"` 通过

---

### [S2.2] C2 — Users 表名漂移修复

**根因**：`UserConfiguration.cs:16` 映射 `ToTable("Users")`，但 `AddIdentityTables` 迁移创建的表名为 `AspNetUsers`。之前的迁移（InitialCreate 等）也使用 "Users"，Identity 迁移创建了不同名称的表。

**修复策略**（实施时需验证选择）：

- **方案 X（推荐）**：修改 `UserConfiguration` 映射为 `ToTable("AspNetUsers")` 以匹配已有迁移链。理由：现有数据已在 `AspNetUsers` 表中，改模型映射比重命名表更安全（不涉及数据迁移）。
- **方案 Y**：补一个 `RenameTable("AspNetUsers" → "Users")` 迁移。理由：模型命名更干净，但需要数据迁移。

实施时先检查当前数据库实际表名（`SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%User%'`），选择与实际数据匹配的方案。

**验证**：`dotnet build` 通过；`dotnet ef migrations has-pending-model-changes --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI` 无 pending change

---

### [S2.3] C3 — Local EnsureCreated → MigrateAsync

**根因**：`LocalWebApiProgram.cs:127` 用 `EnsureCreatedAsync()` 建 LocalDB。`EnsureCreated` 不使用迁移链，产生的库无法接收后续 `MigrateAsync` 修复。

**修复**：`LocalWebApiProgram.cs:127` 改为：
```csharp
await dbContext.Database.MigrateAsync();
```

**风险**：如果现有 LocalDB 是由 `EnsureCreated` 创建的，直接 `MigrateAsync` 会失败（无迁移历史表）。需要先删除旧 LocalDB 文件（`%LOCALAPPDATA%\Microsoft\SQL Server LocalDB\Instances\MSSQLLocalDB\` 下的 LYBTDesktop 文件），让 `MigrateAsync` 从头创建。

**验证**：`dotnet build` 通过；删除旧 LocalDB 后启动 LocalWebAPI，LocalDB 通过迁移建库成功

---

### [S2.4] C4 — PrintingModule 注册修复

**根因**：`PrintingModule`（`LYBT.Desktop.Printing/PrintingModule.cs`）注册了 `IPrintService<PrescriptionPrintModel>`，但 `App.xaml.cs` 的 `ConfigureModuleCatalog` 从未 `AddModule<PrintingModule>()`。`ServiceCollectionExtensions.cs:155` 的注释 "由 PrintingModule 注册，此处不重复" 是误导性残留。

**修复**：
1. `App.xaml.cs` 的 `ConfigureModuleCatalog` 添加：`catalog.AddModule<PrintingModule>();`
2. `Shell/Extensions/ServiceCollectionExtensions.cs:155` 删除误导注释

**验证**：`dotnet build` 通过；`PrintingModule` 出现在 ModuleCatalog 中

---

### [S2.5] C5 — Desktop→Entities 跨层引用修复

**根因**：`Desktop.Infrastructure.csproj:81` 直接引用 `Server/Core/LYBT.Entities`，违反「Client 永不引用 Server」。`DesktopLayerArchTests.cs:35` 用 `NotHaveDependencyOnAll`（禁止**同时**依赖两者）而非 `NotHaveDependencyOnAny`（禁止**任一**依赖），使违规合法通过。

**修复**：
1. 将 `src/Server/Core/LYBT.Entities/` 下沉到 `src/Shared/LYBT.Entities/`
   - Entities 仅含 POCO 实体类（ApplicationUser、Patient、Herb 等），无 Server 依赖
   - 下沉后 Server/Desktop/Shared 均可引用，依赖方向合规
2. `LYBTZYZS.sln` 更新项目路径
3. 所有 `.csproj` 的 `ProjectReference` 更新路径（Server + Desktop + Shared 下引用 Entities 的项目）
4. `DesktopLayerArchTests.cs:35` 改为 `NotHaveDependencyOnAny`

**注意**：LocalWebAPI 对 Server 的引用（ADR-0010 特许）不受影响——Entities 下沉后 LocalWebAPI 引用 Shared/Entities 也不再违反分层规则。

**验证**：`dotnet build` 通过；`dotnet test tests/LYBT.Tests.Architecture/ --filter "Desktop"` 通过；无 Desktop→Server 直接引用

---

### [S2.6] H1 — 环境判定 fail-open 修复

**根因**：`DatabaseInitializationService.cs:211` 环境变量 `ASPNETCORE_ENVIRONMENT` 缺失时视为 `Development`，绕过生产门控逻辑（`:146-159`）。

**修复**：缺省值改为 `"Production"`。环境变量缺失 = 保守处理 = 走生产门控。`appsettings.json` 的 `ASPNETCORE_ENVIRONMENT` 配置不变（开发环境显式设为 `Development`）。

**验证**：`dotnet build` 通过；环境变量缺失时行为等同 Production

## [S3] Out of Scope

- HIGH: 多 DbContext 同表混用 → Phase 2
- HIGH: MedicalCase 双写路径死服务 → Phase 2
- HIGH: Controller 成对重复（10 对）→ Phase 2
- MEDIUM: AsNoTracking 缺失、N+1、死代码、静态可变状态等 → Phase 3
- MEDIUM: MVVM 双栈混用、VM 数据入口统一 → Phase 3

## [S4] Tasks

- [ ] T1: C1 密码哈希统一 — 改两个 Handler 走 UserManager，验证 build + 相关测试 (covers: S2.1)
- [ ] T2: C2 Users 表名修复 — 验证当前 DB 实际表名，选择修复方案，实施+验证 (covers: S2.2)
- [ ] T3: C3 Local MigrateAsync — 改 LocalWebApiProgram，清理旧 LocalDB，验证 (covers: S2.3)
- [ ] T4: C4 PrintingModule 注册 — App.xaml.cs 补注册，删误导注释，验证 (covers: S2.4)
- [ ] T5: C5 Entities 下沉 — 移动项目+更新引用+修复架构测试，验证 (covers: S2.5)
- [ ] T6: H1 环境判定 — 改缺省值为 Production，验证 (covers: S2.6)
- [ ] T7: 全量验证 — `dotnet build` + `dotnet test tests/LYBT.Tests.Architecture/` 全通过
