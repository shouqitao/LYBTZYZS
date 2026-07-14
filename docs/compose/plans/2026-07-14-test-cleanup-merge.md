# 测试清理合并实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 清理冗余的 C# 集成测试（已被 Newman API 测试覆盖），保留单元测试、架构测试和必要的集成测试。

**Architecture:** 删除 `tests/LYBT.Tests.Server/Integration/` 和 `tests/LYBT.Tests.Server/UserJourneys/` 目录下的所有测试文件，这些测试已被 Newman API 测试（86 个端点）完全覆盖。

**Tech Stack:** .NET 8, C#, xUnit, Newman/Postman

## Global Constraints

- Newman API 测试（86 个端点）是主要的集成测试层
- C# 集成测试中与 Newman 重复的部分应删除
- 单元测试（validators, mappers, entities, infrastructure）必须保留
- 架构测试必须保留
- 速率限制测试必须保留
- 所有更改必须通过 `dotnet build LYBTZYZS.sln` 验证

---

### Task 1: 识别并删除冗余集成测试

**Covers:** 清理 Integration/ 目录下的所有测试

**Files:**
- Delete: `tests/LYBT.Tests.Server/Integration/` 目录下所有文件

**Interfaces:** None

- [ ] **Step 1: 确认要删除的测试目录**

Run: `Get-ChildItem D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Server\Integration\ -Directory`
Expected: 列出 Auth, Configuration, Diagnostics, ErrorHandling, Formulas, Health, Herbs, Logging, MedicalCases, Patients, Registrations, Reports, Security, Users 等目录

- [ ] **Step 2: 删除 Integration 目录**

```bash
Remove-Item -Recurse -Force "D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Server\Integration\"
```

- [ ] **Step 3: 验证构建**

Run: `dotnet build D:\source\repos\LYBTZYZS\LYBTZYZS.sln --no-restore -v q`
Expected: BUILD SUCCESSFUL, 0 warnings, 0 errors

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "test: remove redundant integration tests covered by Newman API tests"
```

---

### Task 2: 删除冗余用户旅程测试

**Covers:** 清理 UserJourneys/ 目录下的所有测试

**Files:**
- Delete: `tests/LYBT.Tests.Server/UserJourneys/` 目录下所有文件

**Interfaces:** None

- [ ] **Step 1: 确认 UserJourneys 目录存在**

Run: `Test-Path D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Server\UserJourneys`
Expected: True

- [ ] **Step 2: 删除 UserJourneys 目录**

```bash
Remove-Item -Recurse -Force "D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Server\UserJourneys\"
```

- [ ] **Step 3: 验证构建**

Run: `dotnet build D:\source\repos\LYBTZYZS\LYBTZYZS.sln --no-restore -v q`
Expected: BUILD SUCCESSFUL, 0 warnings, 0 errors

- [ ] **Step 4: 提交**

```bash
git add -A
git commit -m "test: remove redundant UserJourney tests covered by Newman API tests"
```

---

### Task 3: 最终验证

**Covers:** 确保所有更改后系统正常

**Files:** None

- [ ] **Step 1: 完整构建验证**

Run: `dotnet build D:\source\repos\LYBTZYZS\LYBTZYZS.sln --no-restore -v q`
Expected: BUILD SUCCESSFUL, 0 warnings, 0 errors

- [ ] **Step 2: 运行 Desktop 单元测试**

Run: `dotnet test D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Desktop\ --no-restore -v q`
Expected: 所有测试通过

- [ ] **Step 3: 运行架构测试**

Run: `dotnet test D:\source\repos\LYBTZYZS\tests\LYBT.Tests.Architecture\ --no-restore -v q`
Expected: 所有测试通过

- [ ] **Step 4: 确认保留的测试**

确认以下测试仍然存在：
- `tests/LYBT.Tests.Server/Unit/` — 单元测试
- `tests/LYBT.Tests.Server/Architecture/` — 架构测试
- `tests/LYBT.Tests.Server/RateLimiting/` — 速率限制测试
- `tests/LYBT.Tests.Desktop/` — Desktop 单元测试

- [ ] **Step 5: 提交（如有遗漏更改）**

```bash
git add -A
git commit -m "test: finalize test cleanup - verify all tests pass"
```

---

## Summary

### Files to Delete
1. `tests/LYBT.Tests.Server/Integration/` — 整个目录（~14个子目录）
2. `tests/LYBT.Tests.Server/UserJourneys/` — 整个目录

### Files to Keep
1. `tests/LYBT.Tests.Server/Unit/` — 单元测试
2. `tests/LYBT.Tests.Server/Architecture/` — 架构测试
3. `tests/LYBT.Tests.Server/RateLimiting/` — 速率限制测试
4. `tests/LYBT.Tests.Desktop/` — Desktop 单元测试

## Self-Review

**1. Spec coverage:** 清理冗余测试，保留必要测试。

**2. Placeholder scan:** 无 TBD/TODO 占位符。

**3. Type consistency:** 仅删除文件，无类型变更。
