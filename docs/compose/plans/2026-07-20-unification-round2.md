# 二次统一实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 合并 Validators 到 Shared.Models、清理 Admin 死代码、澄清 Foundation/Infrastructure 边界、区分 OperationResultDto 命名

**Architecture:** 4 项独立任务，按依赖顺序执行。Validators 合并需更新 csproj 引用；死代码清理直接删除文件；边界文档和命名澄清为纯文档/注释变更。

**Tech Stack:** .NET 8, FluentValidation, Prism

## Global Constraints

- `dotnet build LYBTZYZS.sln` 必须通过
- 所有现有测试必须通过
- 中文业务注释，英文标识符
- 最小改动原则

---

### Task 1: Validators 合并到 Shared.Models

**Covers:** [S2]

**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/LYBT.Shared.Models.csproj`
- Delete: `src/Shared/LYBT.Shared.Models/Validators/LYBT.Shared.Validators.csproj`
- Delete: `src/Shared/LYBT.Shared.Models/Validators/bin/` (整个目录)
- Delete: `src/Shared/LYBT.Shared.Models/Validators/obj/` (整个目录)
- Modify: 所有引用 `LYBT.Shared.Validators` 的 csproj

- [ ] **Step 1: 查找所有引用 LYBT.Shared.Validators 的项目**

Run:
```powershell
Get-ChildItem -Recurse -Filter "*.csproj" | Select-String "LYBT.Shared.Validators" | ForEach-Object { $_.Path }
```

Expected: 列出所有引用此项目的 csproj 文件

- [ ] **Step 2: 给 Shared.Models 添加 FluentValidation 包引用**

在 `LYBT.Shared.Models.csproj` 的 `<ItemGroup>` 中添加：
```xml
<PackageReference Include="FluentValidation" />
```

- [ ] **Step 3: 更新引用方 csproj**

将所有引用 `LYBT.Shared.Validators` 的 ProjectReference 改为引用 `LYBT.Shared.Models`（如果尚未引用）。

对于已引用 Shared.Models 的项目，只需删除 Validators 的 ProjectReference。

- [ ] **Step 4: 删除 Validators 子项目文件**

Run:
```powershell
Remove-Item "src\Shared\LYBT.Shared.Models\Validators\LYBT.Shared.Validators.csproj"
Remove-Item -Recurse -Force "src\Shared\LYBT.Shared.Models\Validators\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "src\Shared\LYBT.Shared.Models\Validators\obj" -ErrorAction SilentlyContinue
```

- [ ] **Step 5: 验证编译**

Run:
```powershell
dotnet build LYBTZYZS.sln
```

Expected: 0 errors, 项目数从 34 降到 33

- [ ] **Step 6: 提交**

```powershell
git add -A
git commit -m "refactor(shared): merge LYBT.Shared.Validators into Shared.Models"
```

---

### Task 2: Admin 死代码 ManagementView 清理

**Covers:** [S3]

**Files:**
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/FormulaManagementView.xaml`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/FormulaManagementView.xaml.cs`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/HerbManagementView.xaml`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/HerbManagementView.xaml.cs`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/MedicalCaseManagementView.xaml`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/MedicalCaseManagementView.xaml.cs`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/PatientManagementView.xaml`
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/PatientManagementView.xaml.cs`

- [ ] **Step 1: 确认死代码（二次验证）**

Run:
```powershell
# 检查 AdminModule 和 SysadminModule 是否注册了这些 View
Select-String -Path "src\Client\Desktop\Roles\LYBT.Desktop.Admin\AdminModule.cs" -Pattern "FormulaManagementView|HerbManagementView|MedicalCaseManagementView|PatientManagementView"
Select-String -Path "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Sysadmin\SysadminModule.cs" -Pattern "FormulaManagementView|HerbManagementView|MedicalCaseManagementView|PatientManagementView"
```

Expected: 无匹配（确认是死代码）

- [ ] **Step 2: 删除 8 个文件**

Run:
```powershell
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\FormulaManagementView.xaml"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\FormulaManagementView.xaml.cs"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\HerbManagementView.xaml"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\HerbManagementView.xaml.cs"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\MedicalCaseManagementView.xaml"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\MedicalCaseManagementView.xaml.cs"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\PatientManagementView.xaml"
Remove-Item "src\Client\Desktop\Roles\LYBT.Desktop.Admin\Views\PatientManagementView.xaml.cs"
```

- [ ] **Step 3: 验证编译**

Run:
```powershell
dotnet build LYBTZYZS.sln
```

Expected: 0 errors

- [ ] **Step 4: 提交**

```powershell
git add -A
git commit -m "refactor(admin): remove 4 unused ManagementView files (dead code)"
```

---

### Task 3: Foundation vs Infrastructure 边界文档

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/AGENTS.md`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/AGENTS.md`

- [ ] **Step 1: 更新 Foundation AGENTS.md**

在现有 AGENTS.md 中添加边界说明节：

```markdown
## 职责边界

**Foundation = 无头运行时层**，不依赖 WPF/Prism。

| 文件夹 | 职责 |
|--------|------|
| Http/ | HTTP 客户端（SwitchingApiClient, Refit clients, ApiClients） |
| Security/ | Token 管理、认证状态机、凭证存储 |
| Caching/ | 桌面端缓存管理 |
| HealthCheck/ | API 健康检查 |
| Application/ | 应用状态服务 |
| Modules/ | 模块加载服务 |
| Repositories/ | API 客户端仓储基类 |
| Services/ | 连接模式服务 |

**规则**：Foundation 中的代码不得引用 WPF 类型（Window, UserControl, DependencyObject 等）。
```

- [ ] **Step 2: 更新 Infrastructure AGENTS.md**

在现有 AGENTS.md 中添加边界说明节：

```markdown
## 职责边界

**Infrastructure = WPF/MVVM 基础设施层**，依赖 WPF + Prism。

| 文件夹 | 职责 |
|--------|------|
| Behaviors/ | XAML 行为 |
| CardReader/ | 读卡器集成 |
| Commands/ | MVVM 命令 |
| Configuration/ | WPF 配置 |
| Constants/ | 常量定义 |
| DependencyInjection/ | DI 注册 |
| Events/ | 事件聚合器 |
| Extensions/ | 扩展方法 |
| Helpers/ | 工具类 |
| Http/ | HTTP 辅助（ApiResponseHelper, LoggingHttpHandler） |
| Interfaces/ | 接口定义 |
| LocalData/ | 本地数据存储 |
| Logging/ | 日志辅助 |
| Models/ | UI 模型 |
| Navigation/ | 导航服务 |
| Performance/ | 性能监控 |
| Repositories/ | UI 仓储 |
| Roles/ | 角色定义 |
| Security/ | 安全辅助（SensitiveInfoFilter） |
| Services/ | UI 服务 |
| ViewModels/ | 共享 ViewModel |
| Views/ | 共享 View |
| Windows/ | 窗口 |

**规则**：Infrastructure 可引用 Foundation，但 Foundation 不得引用 Infrastructure。
```

- [ ] **Step 3: 提交**

```powershell
git add "src\Client\Desktop\Core\LYBT.Desktop.Foundation\AGENTS.md" "src\Client\Desktop\Core\LYBT.Desktop.Infrastructure\AGENTS.md"
git commit -m "docs: clarify Foundation vs Infrastructure responsibility boundaries"
```

---

### Task 4: OperationResultDto vs Result<T> 命名澄清

**Covers:** [S5]

**Files:**
- Modify: `src/Shared/LYBT.Shared.Models/Contracts/Common/OperationResultDto.cs`
- Modify: `src/Shared/LYBT.Shared.Models/Contracts/Common/Result.cs`

- [ ] **Step 1: 更新 OperationResultDto 注释**

```csharp
/// <summary>
/// 批量操作结果的基础 DTO — 用于 API 响应层。
/// 注意：这是 API 批量操作（导入/删除）的响应结构，非领域操作结果。
/// 领域操作结果请使用 <see cref="Result{T}"/>。
/// </summary>
public class OperationResultDto
```

- [ ] **Step 2: 更新 Result<T> 注释**

```csharp
/// <summary>
/// 操作结果封装。用于命令和查询的统一返回类型。
/// 这是领域操作结果模式，用于 Service/Repository 层。
/// API 批量操作的响应结构请使用 <see cref="OperationResultDto"/>。
/// </summary>
public class Result<T>
```

- [ ] **Step 3: 验证编译**

Run:
```powershell
dotnet build LYBTZYZS.sln
```

Expected: 0 errors

- [ ] **Step 4: 提交**

```powershell
git add "src\Shared\LYBT.Shared.Models\Contracts\Common\OperationResultDto.cs" "src\Shared\LYBT.Shared.Models\Contracts\Common\Result.cs"
git commit -m "docs: clarify OperationResultDto vs Result<T> naming distinction"
```

---

## 验证清单

- [ ] `dotnet build LYBTZYZS.sln` — 0 errors
- [ ] `dotnet test tests/LYBT.Tests.Desktop/` — 全部通过
- [ ] `dotnet test tests/LYBT.Tests.Server/` — 全部通过
- [ ] `dotnet test tests/LYBT.Tests.Architecture/` — 全部通过
- [ ] 项目数 = 33
