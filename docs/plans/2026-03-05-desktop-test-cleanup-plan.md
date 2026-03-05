# Desktop 测试清理 Phase A 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 删除 49 个低价值 mock 测试，保留 27 个高价值测试，使所有剩余测试都能在代码出错时变红。

**Architecture:** 删除 pass-through mock 测试和 Received()/DidNotReceive() 交互验证测试。保留状态验证、事件验证、null guard 和 IsNavigationTarget 测试。不改任何业务代码。

**Tech Stack:** xUnit, NSubstitute, FluentAssertions, .NET 8

---

## 基线

- Desktop 测试数: 503 (含 ViewModels/ 69 个)
- 预期删除: 49 个 (2 整文件 + 3 文件部分清理)
- 预期保留: 454 个
- 全量基线: Server 1017 + Desktop 454 + Arch 76 = 1547

---

### Task 1: 删除 PatientRepositoryTests.cs (整文件)

**Files:**
- Delete: `tests/LYBT.Tests.Desktop/ViewModels/Patients/PatientRepositoryTests.cs`

**理由:** 全部 14 个测试都是 mock DataSource 返回 X，断言 Repository 返回 X 的 pass-through 测试。Patient CRUD 已被 `EndToEnd/Patients/PatientE2ETests.cs` (6 tests) 和 `EndToEnd/LocalMode/DataSourceIntegrationTests.cs` (12 tests) 完整覆盖。

**Step 1: 确认 E2E 覆盖存在**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~PatientE2ETests" -v minimal
```

Expected: 6 tests passed

**Step 2: 删除文件**

```bash
cd "D:/source/repos/LYBTZYZS"
git rm tests/LYBT.Tests.Desktop/ViewModels/Patients/PatientRepositoryTests.cs
```

**Step 3: 验证编译**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
```

Expected: 0 errors

**Step 4: 删除空目录 (如果为空)**

```bash
rmdir tests/LYBT.Tests.Desktop/ViewModels/Patients 2>/dev/null || true
```

---

### Task 2: 删除 UserRepositoryTests.cs (整文件)

**Files:**
- Delete: `tests/LYBT.Tests.Desktop/ViewModels/Users/UserRepositoryTests.cs`

**理由:** 全部 17 个测试都是 pass-through mock 测试。User CRUD 已被 `EndToEnd/Users/UserE2ETests.cs` (3 tests) 和 `EndToEnd/LocalMode/DataSourceIntegrationTests.cs` 完整覆盖。

**Step 1: 确认 E2E 覆盖存在**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~UserE2ETests" -v minimal
```

Expected: 3 tests passed

**Step 2: 删除文件**

```bash
git rm tests/LYBT.Tests.Desktop/ViewModels/Users/UserRepositoryTests.cs
```

**Step 3: 验证编译**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
```

Expected: 0 errors

**Step 4: 删除空目录 (如果为空)**

```bash
rmdir tests/LYBT.Tests.Desktop/ViewModels/Users 2>/dev/null || true
```

---

### Task 3: 清理 AdminHomeViewModelTests.cs (删 4 保 6)

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/ViewModels/Admin/AdminHomeViewModelTests.cs`

**删除 4 个导航 mock 测试** (lines 109-165):
- `NavigateToUserManagementCommand_ShouldNavigate` (line 111-122)
- `NavigateToHerbManagementCommand_ShouldNavigate` (line 124-135)
- `NavigateToSystemSettingsCommand_ShouldNavigate` (line 137-148)
- `ChangePasswordCommand_ShouldNavigateWithTabParameter` (line 150-163)

**保留 6 个高价值测试**:
- 4 个构造函数 null guard (lines 55-88)
- 1 个初始状态验证 (lines 94-105)
- 1 个 IsNavigationTarget (lines 169-183)

**Step 1: 删除导航命令测试 region**

删除 `#region 导航命令测试` 到 `#endregion` 之间的全部内容 (lines 109-165)。

**Step 2: 验证编译和测试**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~AdminHomeViewModelTests" -v minimal
```

Expected: 6 tests passed (was 10)

---

### Task 4: 清理 ClinicalHomeViewModelTests.cs (删 4 保 6)

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/ViewModels/Clinical/ClinicalHomeViewModelTests.cs`

**删除 4 个导航 mock 测试** (lines 107-163):
- `StartMedicalCaseCommand_ShouldNavigateToPatientSelection` (line 109-120)
- `NavigateToPatientManagementCommand_ShouldNavigate` (line 122-133)
- `NavigateToHerbLibraryCommand_ShouldNavigate` (line 135-146)
- `ChangePasswordCommand_ShouldNavigateWithTabParameter` (line 148-161)

**保留 6 个高价值测试**:
- 4 个构造函数 null guard (lines 55-88)
- 1 个初始状态验证 (lines 94-103)
- 1 个 IsNavigationTarget (lines 167-181)

**Step 1: 删除导航命令测试 region**

删除 `#region 导航命令测试` 到 `#endregion` 之间的全部内容 (lines 107-163)。

**Step 2: 验证编译和测试**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~ClinicalHomeViewModelTests" -v minimal
```

Expected: 6 tests passed (was 10)

---

### Task 5: 清理 LoginCoordinatorTests 主类 (删 8 保 12)

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/ViewModels/Shell/Login/LoginCoordinatorTests.cs`

**删除 8 个 Received()/DidNotReceive() 测试** (主类 LoginCoordinatorTests):

| 方法名 | 行号 | 删除理由 |
|--------|------|---------|
| `LoginAsync_Success_ShouldStartSession` | 170-184 | `_sessionManager.Received(1).StartSessionAsync(...)` |
| `LoginAsync_Success_ShouldLoadModules` | 186-200 | `_moduleLoading.Received(1).LoadModulesAsync(...)` |
| `LoginAsync_AdminUser_ShouldLoadAdminModules` | 202-219 | `_moduleLoading.Received(1).LoadModulesAsync(...)` |
| `HandleLoginSuccessAsync_ShouldStartSession` | 312-326 | `_sessionManager.Received(1).StartSessionAsync(...)` |
| `HandleLoginSuccessAsync_ShouldLoadModules` | 328-341 | `_moduleLoading.Received().LoadModulesAsync(...)` |
| `LogoutAsync_ShouldEndSession` | 375-389 | `_sessionManager.Received(1).EndSessionAsync()` |
| `LogoutAsync_ShouldCallAuthServiceLogout` | 391-405 | `_authService.Received(1).LogoutAsync()` |

注意: `HandleLoginSuccessAsync_WithNullUser_ShouldThrow` (343-351) 是 null guard，**保留**。

**保留 12 个高价值测试**:
- `Constructor_ShouldInitialize_WithNotLoggedInState` (状态验证)
- `Constructor_WithNullLogger_ShouldThrow` (null guard)
- `Constructor_WithNullAuthService_ShouldThrow` (null guard)
- `LoginAsync_Success_ShouldTransitionToLoggedIn` (状态转换)
- `LoginAsync_Failure_ShouldReturnFailedResult` (状态转换)
- `LoginAsync_ShouldRaiseStateChangedEvents` (事件验证)
- `LoginAsync_Success_ShouldRaiseLoginSucceededEvent` (事件验证)
- `LoginAsync_WithInvalidUsername_ShouldThrow` (输入验证)
- `LoginAsync_WithInvalidPassword_ShouldThrow` (输入验证)
- `HandleLoginSuccessAsync_WithNullUser_ShouldThrow` (null guard)
- `LogoutAsync_ShouldTransitionToNotLoggedIn` (状态转换)
- `LogoutAsync_ShouldRaiseLogoutCompletedEvent` (事件验证)
- `GetDiagnostics_Initial_ShouldReturnNotLoggedInState` (状态验证)
- `GetDiagnostics_AfterLogin_ShouldReturnUserInfo` (状态验证)

**Step 1: 逐个删除上述 7 个 Received() 测试方法**

按从后往前的顺序删除 (避免行号偏移):
1. 先删 `LogoutAsync_ShouldCallAuthServiceLogout` (391-405)
2. 再删 `LogoutAsync_ShouldEndSession` (375-389)
3. 再删 `HandleLoginSuccessAsync_ShouldLoadModules` (328-341)
4. 再删 `HandleLoginSuccessAsync_ShouldStartSession` (312-326)
5. 再删 `LoginAsync_AdminUser_ShouldLoadAdminModules` (202-219)
6. 再删 `LoginAsync_Success_ShouldLoadModules` (186-200)
7. 再删 `LoginAsync_Success_ShouldStartSession` (170-184)

**Step 2: 清理 helper methods**

删除 `SetupSuccessfulAutoLogin` 方法 (495-507)，已无测试引用。保留 `SetupSuccessfulLogin`、`SetupNavigationService`、`CreateTestUser`、`CreateLoginResponse`。

**Step 3: 验证编译和测试**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~LoginCoordinatorTests" -v minimal
```

Expected: 主类 12 tests passed (was 19-20)

---

### Task 6: 清理 LoginCoordinatorLocalModeTests (删 2 保 3)

**Files:**
- Modify: `tests/LYBT.Tests.Desktop/ViewModels/Shell/Login/LoginCoordinatorTests.cs` (同文件, 第二个类)

**删除 2 个 DidNotReceive() 测试**:

| 方法名 | 行号 | 删除理由 |
|--------|------|---------|
| `LoginAsync_LocalMode_ShouldNotCallRemoteAuthService` | 633-652 | `_authService.DidNotReceive().LoginAsync(...)` |
| `LoginAsync_LocalMode_ShouldNotSaveToken` | 654-674 | `_tokenStorage.DidNotReceive().SaveAuthenticationAsync(...)` |

**保留 3 个高价值测试**:
- `LoginAsync_LocalMode_Success_ShouldTransitionToAuthenticated` (真实状态转换)
- `LoginAsync_LocalMode_InvalidCredentials_ShouldReturnFailed` (真实状态转换)
- `LoginAsync_LocalMode_WithoutLocalAuthService_ShouldReturnFailed` (真实错误处理)

**Step 1: 删除 2 个 DidNotReceive() 测试**

**Step 2: 验证编译和测试**

```bash
dotnet build tests/LYBT.Tests.Desktop/ -v q
dotnet test tests/LYBT.Tests.Desktop/ --no-build --filter "FullyQualifiedName~LoginCoordinatorLocalModeTests" -v minimal
```

Expected: 3 tests passed (was 5)

---

### Task 7: 全量验证 + 提交

**Step 1: 全量编译**

```bash
dotnet build LYBT.All.sln -v q
```

Expected: 0 errors

**Step 2: 全量测试**

```bash
dotnet test tests/LYBT.Tests.Desktop/ -v minimal
```

Expected: ~454 tests, 0 failures (原 503 - 删 49)

```bash
dotnet test tests/LYBT.Tests.Server/ --no-build -v minimal
```

Expected: 1017 tests, 0 failures (不受影响)

```bash
dotnet test tests/LYBT.Tests.Architecture/ --no-build -v minimal
```

Expected: 76 tests, 0 failures (不受影响)

**Step 3: 确认 git 状态**

```bash
git status
git diff --stat
```

Expected changes:
- Deleted: `ViewModels/Patients/PatientRepositoryTests.cs`
- Deleted: `ViewModels/Users/UserRepositoryTests.cs`
- Modified: `ViewModels/Admin/AdminHomeViewModelTests.cs`
- Modified: `ViewModels/Clinical/ClinicalHomeViewModelTests.cs`
- Modified: `ViewModels/Shell/Login/LoginCoordinatorTests.cs`

**Step 4: Commit**

```bash
git add -A tests/LYBT.Tests.Desktop/ViewModels/
git commit -m "test: remove 49 low-value mock tests from Desktop ViewModels

Delete pass-through repository mock tests (PatientRepositoryTests,
UserRepositoryTests) and Received()/DidNotReceive() interaction
verification tests from AdminHome, ClinicalHome, and LoginCoordinator.

These tests verified mock wiring rather than real behavior - they would
pass even when business code was broken. Real behavior is already
covered by EndToEnd tests using DesktopFixture with SQLite InMemory.

Retained: null guards, state transitions, event verification, navigation
protocol tests - all verify real observable behavior."
```

---

## Verification Checklist

- [ ] Server tests: 1017 passed, 0 failed
- [ ] Desktop tests: ~454 passed, 0 failed
- [ ] Architecture tests: 76 passed, 0 failed
- [ ] No business code changed
- [ ] All deleted tests were mock-only (no real behavior verification lost)
- [ ] Remaining tests all verify observable state, events, or exceptions
