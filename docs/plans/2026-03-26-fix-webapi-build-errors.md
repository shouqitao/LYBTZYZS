# Fix WebAPI Build Errors Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 HerbsController 和 PatientsController 中的 25 个编译错误，使 WebAPI 项目构建成功。

**Architecture:** 显式指定泛型类型参数解决 CS0411 推断错误，为缺失 CancellationToken 参数的方法添加参数。

**Tech Stack:** ASP.NET Core 8, C# 12, .NET 8

---

## 错误分析

| 错误类型 | 文件 | 行号 | 根因 |
|----------|------|------|------|
| CS0411 | HerbsController.cs | 103, 128, 350 | `GetEntityWithOwnershipCheckAsync` 调用未指定泛型参数 |
| CS8130/CS8183 | HerbsController.cs | 103, 128, 350 | 弃元类型依赖泛型推断 |
| CS0103 | HerbsController.cs | 428 | `BatchDisable` 方法无 `CancellationToken` 参数但使用了 `cancellationToken` |
| CS0411 | PatientsController.cs | 123, 155, 267 | 同上泛型推断问题 |
| CS8130/CS8183 | PatientsController.cs | 123, 155, 267 | 同上弃元推断问题 |

---

## Phase 1: 修复 HerbsController (16 错误)

### Task 1.1: 修复 HerbsController.Update 方法 (行 103)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:103`

**Step 1: 读取当前代码**

行 103 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
```

**Step 2: 显式指定泛型类型参数**

需要确定 `GetByIdAsync` 返回的 DTO 类型。从 HerbsController 第 67 行的 `GetById` 方法可知：
```csharp
var result = await _herbService.GetByIdAsync(id, cancellationToken);
```
返回 `Result<HerbDetailDto>`（见第 62 行 `[ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]`）。

修改行 103：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, _herbService.GetByIdAsync, "药材");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 103 相关的 CS0411/CS8130/CS8183 错误消失

**Step 4: 暂存修改（不提交，等所有修复完成后统一提交）**

---

### Task 1.2: 修复 HerbsController.Delete 方法 (行 128)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:128`

**Step 1: 读取当前代码**

行 128 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
```

**Step 2: 显式指定泛型类型参数**

修改行 128：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, _herbService.GetByIdAsync, "药材");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 128 相关的错误消失

---

### Task 1.3: 修复 HerbsController.ToggleStatus 方法 (行 350)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:350`

**Step 1: 读取当前代码**

行 350 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");
```

**Step 2: 显式指定泛型类型参数**

修改行 350：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, _herbService.GetByIdAsync, "药材");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 350 相关的错误消失

---

### Task 1.4: 修复 HerbsController.BatchDisable 方法 (行 428)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs:420`

**Step 1: 读取当前代码**

行 420 当前方法签名：
```csharp
public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto)
```

行 428 当前代码（使用了未定义的 `cancellationToken`）：
```csharp
var result = await _herbService.BatchUpdateStatusAsync(dto.Ids, CommonStatus.Disabled, cancellationToken);
```

**Step 2: 添加 CancellationToken 参数**

修改方法签名为：
```csharp
public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto dto, CancellationToken cancellationToken = default)
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: CS0103 错误消失

---

## Phase 2: 修复 PatientsController (9 错误)

### Task 2.1: 修复 PatientsController.Update 方法 (行 123)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:123`

**Step 1: 读取当前代码**

行 123-124 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
    id, _service.GetByIdAsync, "患者");
```

**Step 2: 确定 DTO 类型**

从 PatientsController 第 70 行的 `GetById` 方法可知返回 `PatientDetailDto`。

修改行 123-124：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(
    id, _service.GetByIdAsync, "患者");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 123 相关错误消失

---

### Task 2.2: 修复 PatientsController.Delete 方法 (行 155)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:155`

**Step 1: 读取当前代码**

行 155-156 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(
    id, _service.GetByIdAsync, "患者");
```

**Step 2: 显式指定泛型类型参数**

修改行 155-156：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(
    id, _service.GetByIdAsync, "患者");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 155 相关错误消失

---

### Task 2.3: 修复 PatientsController.ToggleStatus 方法 (行 267)

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs:267`

**Step 1: 读取当前代码**

行 267 当前代码：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "患者");
```

**Step 2: 显式指定泛型类型参数**

修改行 267：
```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(id, _service.GetByIdAsync, "患者");
```

**Step 3: 验证修改**

运行: `dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore`
Expected: 行 267 相关错误消失

---

## Phase 3: 验证与提交

### Task 3.1: 完整构建验证

**Step 1: 运行 WebAPI 项目构建**

```bash
dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj --no-restore
```

Expected: 0 errors, 0 warnings

**Step 2: 运行 MedicalCase 模块构建**

```bash
dotnet build src/Server/Modules/LYBT.Module.MedicalCase/LYBT.Module.MedicalCase.csproj --no-restore
```

Expected: 0 errors, 0 warnings

**Step 3: 运行单元测试（如果项目可构建）**

```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --no-build
```

Expected: All tests pass

---

### Task 3.2: 提交代码

**Step 1: 查看所有修改**

```bash
git status
git diff
```

**Step 2: 提交**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs
git add src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs
git commit -m "fix: resolve 25 WebAPI build errors in HerbsController and PatientsController

- Add explicit generic type parameters to GetEntityWithOwnershipCheckAsync calls
- Add missing CancellationToken parameter to HerbsController.BatchDisable
- Fixes CS0411 (generic type inference), CS8130/CS8183 (discard inference), CS0103 (undefined variable)

Affected methods:
- HerbsController: Update, Delete, ToggleStatus, BatchDisable
- PatientsController: Update, Delete, ToggleStatus"
```

---

### Task 3.3: 推送到远程

```bash
git push origin master
```

Expected: Push successful

---

## 依赖关系

```
Task 1.1 ──┐
Task 1.2 ──┤
Task 1.3 ──┼──> Task 3.1 (构建验证) ──> Task 3.2 (提交) ──> Task 3.3 (推送)
Task 1.4 ──┤
Task 2.1 ──┤
Task 2.2 ──┤
Task 2.3 ──┘
```

Tasks 1.1-2.3 可并行执行（修改不同行），但建议按顺序执行以逐步验证。

---

## 参考文件

- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:411-422` - GetEntityWithOwnershipCheckAsync 签名
- `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs` - 待修复文件
- `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` - 待修复文件

---

## 注意事项

1. **DTO 类型确认**: `HerbDetailDto` 和 `PatientDetailDto` 必须实现 `ICreatorTrackable` 接口（`GetEntityWithOwnershipCheckAsync` 的约束）
2. **CancellationToken 默认值**: 使用 `default` 保持向后兼容
3. **不修改旧 MedicalCaseController**: 该文件已有 `[NonController]` 属性，不在此次修复范围
