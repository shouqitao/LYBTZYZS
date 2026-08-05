# B-02 配置修改 API 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为服务端 `ConfigurationController` 添加 PUT 端点，让 Admin 可以通过 API 修改运行时系统配置，支持 JSON 文件持久化与热更新。

**Architecture:** 新增 `IConfigurationStore` + `JsonFileConfigurationStore` 独立持久化层（不触 AppDbContext，满足 P10）；`ISystemConfigurationService` 增加 SetValueAsync/UpdateConfigurationAsync；`ConfigurationController` 增加两个 PUT 端点；Program.cs 启动时以 `reloadOnChange` 方式追加 runtime-overrides.json 到配置链，Service 写入后 `IConfigurationRoot.Reload()` 触发 `IOptionsMonitor<T>` 热更新。

**Tech Stack:** .NET 8 / ASP.NET Core / IOptionsMonitor / System.Text.Json / xUnit + FluentAssertions

## Global Constraints

- 0 错误 0 警告：`dotnet build LYBTZYZS.sln --no-incremental`
- 架构约束 P10：Service 禁注入 AppDbContext（本方案用独立 Store，天然满足）
- Server 测试禁 mock（NSubstitute/Moq 禁令）— 测试用真实实现 + 临时文件
- 只存储与 appsettings.json 默认值不同的项
- 禁止修改：`ConnectionStrings:DefaultConnection`、`Jwt:SecretKey`、`DefaultPasswords:*`
- 白名单：只允许修改已声明 SectionName 的 Options 对应的配置节
- 完成后单 commit + push，message: `feat(config): add PUT endpoint for runtime configuration update`

---

### Task 1: IConfigurationStore + JsonFileConfigurationStore

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Configuration/Stores/IConfigurationStore.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Configuration/Stores/JsonFileConfigurationStore.cs`
- Test: `tests/LYBT.Tests.Server/Unit/Configuration/JsonFileConfigurationStoreTests.cs`

**Interfaces:**
- Produces: `IConfigurationStore`（LoadAllAsync / SetValueAsync / RemoveAsync / SaveAsync）

- [ ] **Step 1: Write failing tests**（见下方测试文件）
- [ ] **Step 2: Run tests → FAIL**（类型不存在）
- [ ] **Step 3: Write interface + implementation**
- [ ] **Step 4: Run tests → PASS**
- [ ] **Step 5: Commit**

---

### Task 2: ISystemConfigurationService 扩展 + 白名单策略

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Configuration/Services/ISystemConfigurationService.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Configuration/Services/SystemConfigurationService.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Configuration/Security/ConfigurationWritePolicy.cs`

**Interfaces:**
- Consumes: `IConfigurationStore`（Task 1）
- Produces: `ISystemConfigurationService.SetValueAsync(string key, string value, CancellationToken)`, `UpdateConfigurationAsync(Dictionary<string,string>, CancellationToken)`

- [ ] **Step 1: Write failing tests**（SetValue → GetValue 一致；敏感键拒绝；白名单外拒绝）
- [ ] **Step 2: Run tests → FAIL**
- [ ] **Step 3: Implement service methods + policy**
- [ ] **Step 4: Run tests → PASS**
- [ ] **Step 5: Commit**

---

### Task 3: Controller PUT 端点

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs`

**Interfaces:**
- Consumes: `ISystemConfigurationService`（Task 2）

- [ ] **Step 1: Add PUT endpoints**（单 key + 批量）
- [ ] **Step 2: Build → PASS**
- [ ] **Step 3: Commit**

---

### Task 4: Program.cs 启动加载 + DI 注册

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Program.cs`

- [ ] **Step 1: AddJsonFile(runtime-overrides.json, optional, reloadOnChange) + 基线快照 + store/服务注册**
- [ ] **Step 2: Build → PASS**
- [ ] **Step 3: Commit**

---

### Task 5: 文档 + 总账更新

**Files:**
- Modify: `docs/03-architecture/13b-api-endpoints.md`（第 141 行）
- Modify: `docs/03-architecture/13-project-master-plan.md`（B-02 状态）

- [ ] **Step 1: 更新 API 文档表格**
- [ ] **Step 2: 更新总账 B-02 状态**
- [ ] **Step 3: Commit**

---

### Task 6: 全量验证 + push

- [ ] **Step 1: `dotnet build LYBTZYZS.sln --no-incremental` → 0 错误 0 警告**
- [ ] **Step 2: 跑新增单元测试**
- [ ] **Step 3: Commit（若无未提交变更）**
- [ ] **Step 4: `git push`**
