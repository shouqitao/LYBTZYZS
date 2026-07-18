# Desktop P1 架构健壮性实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 4 个 P1 架构健壮性问题，提升 Desktop 端双模式一致性和本地数据演进能力

**Architecture:** 统一 Local/Remote API 路由前缀、LocalDbContext 迁移策略、ILocalApi 方法补齐、ErrorCategory 去重

**Tech Stack:** .NET 8, WPF/Prism.DryIoc, EF Core, Refit, Serilog

## Global Constraints

- 依赖方向: Shell → Roles → Modules → Core → Shared (严格单向)
- 模块间禁止直接引用
- 所有 ViewModel 继承标准基类
- Mapperly 编译期映射，禁止 AutoMapper
- 中文业务文档/注释，英文标识符/commit
- `dotnet build LYBTZYZS.sln` 必须通过

---

### Task 1: Local/Remote API 路由前缀统一

**Covers:** [S7 #3]

**目标:** 将 LocalWebAPI 路由前缀从 `/api/` 统一为 `/api/v1/`，与远程 API 保持一致

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/` 下所有 Controller 的路由前缀
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/ILocal*.cs` 中的 Refit 路由属性
- Test: `dotnet build LYBTZYZS.sln`

**Steps:**

- [ ] **Step 1: 找到所有 LocalWebAPI Controller**

```
搜索: src/Client/Desktop/LocalWebAPI/ 下所有 Controller 文件
确认路由前缀格式: [Route("api/[controller]")] → 需要改为 [Route("api/v1/[controller]")]
```

- [ ] **Step 2: 修改 LocalWebAPI Controller 路由前缀**

```bash
# 在 LocalWebAPI 目录下，将所有 Controller 的路由前缀从 "api/" 改为 "api/v1/"
# 使用 serena_replace_in_files 批量替换
```

- [ ] **Step 3: 修改 ILocal*Api Refit 接口路由**

```
搜索: src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/ILocal*.cs
确认 Refit 属性中的路由: [Refit.Get("/api/...")] → 改为 [Refit.Get("/api/v1/...")]
```

- [ ] **Step 4: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix(LocalWebAPI): 统一 API 路由前缀为 /api/v1/"
```

---

### Task 2: LocalDbContext 改用 MigrateAsync

**Covers:** [S7 #10]

**目标:** 将 LocalDbContext 初始化从 EnsureCreatedAsync 改为 MigrateAsync，支持 schema 演进

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/DatabaseInitializer.cs`
- Test: `dotnet build LYBTZYZS.sln`

**Steps:**

- [ ] **Step 1: 读取当前 DatabaseInitializer 实现**

```csharp
// 当前使用 EnsureCreatedAsync，需改为 MigrateAsync
// 同时需要添加 Migration 文件（如果使用 Code-First Migrations）
```

- [ ] **Step 2: 检查是否存在 LocalData Migration**

```
搜索: src/Client/Desktop/Core/LYBT.Desktop.LocalData/Migrations/
如果没有 Migrations 目录，需要先创建初始 Migration
```

- [ ] **Step 3: 修改 DatabaseInitializer**

```csharp
// 将 EnsureCreatedAsync 替换为 MigrateAsync
await context.Database.MigrateAsync();
```

- [ ] **Step 4: 如果没有 Migration，创建初始 Migration**

```bash
dotnet ef migrations add InitialCreate --project src/Client/Desktop/Core/LYBT.Desktop.LocalData --startup-project src/Client/Desktop/LocalWebAPI
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```
Expected: BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix(LocalData): 改用 MigrateAsync 支持 schema 演进"
```

---

### Task 3: ILocalMedicalCaseApi 方法补齐

**Covers:** [S7 #8]

**目标:** 补齐 ILocalMedicalCaseApi 中缺失的方法，使其与 IMedicalCaseApi 功能对齐

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/ILocalMedicalCaseApi.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/` 中对应的 Controller
- Test: `dotnet build LYBTZYZS.sln`

**Steps:**

- [ ] **Step 1: 对比 IMedicalCaseApi 和 ILocalMedicalCaseApi**

```
列出 IMedicalCaseApi 中有但 ILocalMedicalCaseApi 中没有的方法
确认哪些方法需要在本地模式支持
```

- [ ] **Step 2: 在 ILocalMedicalCaseApi 中添加缺失方法**

```csharp
// 示例: 添加审计日志查询
[Refit.Get("/api/v1/medicalcases/{id}/auditlogs")]
Task<List<MedicalCaseAuditLogDto>> GetAuditLogsAsync(Guid id);
```

- [ ] **Step 3: 在 LocalWebAPI Controller 中实现新方法**

```csharp
[HttpGet("{id}/auditlogs")]
public async Task<ActionResult<List<MedicalCaseAuditLogDto>>> GetAuditLogs(Guid id)
{
    // 实现本地模式的审计日志查询
}
```

- [ ] **Step 4: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix(LocalWebAPI): 补齐 ILocalMedicalCaseApi 缺失方法"
```

---

### Task 4: ErrorCategory/ErrorSeverity 去重

**Covers:** [S7 #2]

**目标:** 移除 LYBT.Shared.Models 中重复的 ErrorCategory/ErrorSeverity 定义，统一使用 Primitives 版本

**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/Enums/ErrorEnums.cs` (移除重复定义)
- Modify: 所有引用 `LYBT.Shared.Models.Enums.ErrorCategory` 的文件 (改为引用 `LYBT.Shared.Primitives.ErrorCodes.ErrorCategory`)
- Test: `dotnet build LYBTZYZS.sln`

**Steps:**

- [ ] **Step 1: 确认重复定义**

```
搜索 ErrorCategory 在 Models/Enums/ErrorEnums.cs 和 Primitives/ErrorCodes/ErrorCategory.cs 中的定义
确认值完全相同
```

- [ ] **Step 2: 找到所有引用 Models.ErrorCategory 的文件**

```
grep -r "LYBT.Shared.Models.Enums.ErrorCategory" --include="*.cs"
```

- [ ] **Step 3: 修改引用为 Primitives 版本**

```
将 using LYBT.Shared.Models.Enums; 改为 using LYBT.Shared.Primitives.ErrorCodes;
或添加 using PrimitivesAlias = LYBT.Shared.Primitives.ErrorCodes;
```

- [ ] **Step 4: 从 ErrorEnums.cs 中移除重复定义**

```csharp
// 移除 ErrorCategory 和 ErrorSeverity 枚举
// 保留 ErrorEnums.cs 中其他不重复的定义
```

- [ ] **Step 5: 验证编译通过**

```bash
dotnet build LYBTZYZS.sln
```
Expected: BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(Shared): 移除 ErrorCategory/ErrorSeverity 重复定义，统一使用 Primitives"
```

---

### Task 5: 验证所有 P1 改进

**Covers:** [S7 #2, #3, #8, #10]

**目标:** 运行完整构建和测试，确认所有 P1 改进无回归

**Steps:**

- [ ] **Step 1: 完整构建**

```bash
dotnet build LYBTZYZS.sln
```
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 运行 Desktop 测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/
```
Expected: 所有测试通过

- [ ] **Step 3: 运行架构测试**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```
Expected: 所有架构约束测试通过

- [ ] **Step 4: 验证双模式切换**

```
启动 Desktop 应用
测试 Local 模式 (localhost:5300) 功能正常
测试 Remote 模式 (远程 API) 功能正常
确认路由前缀统一后无 404 错误
```
Expected: 双模式均正常工作
